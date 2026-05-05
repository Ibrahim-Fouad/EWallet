# Feature: Money Transfer

## Business Rules

- Transfer moves funds from source wallet to destination wallet using recipient's `PhoneNumber`
- **Self-transfer is forbidden** — `sourceWalletId == destinationWalletId` returns a validation error
- **Single-currency only** — source and destination must have the same currency
- Transfer amount must be positive
- Source wallet must have sufficient balance (insufficient funds → business error, not exception)
- Transfers are **append-only** — never deleted or updated

## Saga Steps (Orchestrated in TransferCommandHandler)

1. Check idempotency key → return cached result if already processed
2. Resolve source wallet (authenticated user's wallet)
3. Resolve destination wallet (by PhoneNumber via `IWalletLookupService`)
4. Validate: not same wallet, same currency, amount > 0
5. Acquire Redis distributed locks (sorted by wallet ID to prevent deadlock)
6. Debit source wallet
7. Credit destination wallet
8. Save wallet changes (optimistic concurrency via `RowVersion`)
9. Create `Transaction` + `TransactionEntry` records (in same DB transaction as wallet changes)
10. Publish `TransferCompletedEvent`
11. Release Redis locks (always in `finally`)
12. Store idempotency result

## Compensation

- If credit fails after debit committed → reverse the debit by calling `source.Credit(amount)` and saving
- If `DbUpdateConcurrencyException` → release locks, return retriable failure (client retries with same `IdempotencyKey`)
- If Redis lock times out → return `WalletLocked` error

## Rate Limiting

- Max **10 transfers per minute** per user (sliding window)
- Applied directly on the Transfer endpoint

## Edge Cases

- Self-transfer check: compare `sourceWalletId == destinationWalletId` (NOT owner IDs — a user can own multiple wallets)
- `DbUpdateConcurrencyException` is retriable — return a specific error code so client can retry with the same `IdempotencyKey`
- Lock acquisition order: always acquire in alphabetical order by `walletId.ToString()` to prevent deadlock between concurrent opposite-direction transfers

## Modules Involved

- `Transactions` — primary module
- `Wallets` — balance changes happen here via `IWalletRepository`
- `Notifications` — receives `TransferCompletedEvent` via MassTransit
