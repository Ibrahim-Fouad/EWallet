# Feature: Wallet Management

## Business Rules

- A user can own **up to 3 wallets** (enforced in `CreateWalletCommandHandler`)
- Each wallet has a unique `PhoneNumber` across the entire system (enforced via DB unique index)
- New wallets automatically receive a **welcome bonus of 10** (EGP or USD, matching wallet currency) from the system wallet
- Wallets are **single-currency** — no cross-currency balance
- Wallet fields: `Id`, `OwnerId`, `PhoneNumber`, `Balance`, `Currency`, `RowVersion`, `IsActive`, `CreatedAt`
- System wallets (`SYSTEM-EGP`, `SYSTEM-USD`) are seeded in the first migration and must never appear in user-facing API responses
- System user ID: `00000000-0000-0000-0000-000000000001`
- System EGP wallet ID: `00000000-0000-0000-0000-000000000001`
- System USD wallet ID: `00000000-0000-0000-0000-000000000002`

## Edge Cases

- Creating a 4th wallet returns a validation error (not an exception)
- Depositing 0 or a negative amount returns a validation error
- PhoneNumber collision returns a conflict error (the DB unique constraint will throw — map to 409)
- The welcome bonus transfer is recorded as a transaction from system wallet to the new wallet

## Modules Involved

- `Wallets` — primary module
- `Transactions` — records the welcome bonus transfer (introduced in Step 7)
- `Identity` — `OwnerId` references the identity user

## Key Decisions

- `RowVersion` mapped as SQL Server `rowversion` (EF Core `IsRowVersion()`) for optimistic concurrency
- Balance has a private setter — only `Deposit()`, `Debit()`, `Credit()` methods modify it
- `IWalletLookupService` interface lives in BuildingBlocks so Transactions module can resolve PhoneNumber → wallet without circular dependency
- System wallets filtered from all user queries via `OwnerId != SystemConstants.SystemUserId`
- Read queries use `AsNoTracking()` for performance
