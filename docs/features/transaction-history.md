# Feature: Transaction History

## Business Rules

- Users can view all transactions on their wallet(s)
- Results are **paginated** (default page size: 20, max: 100)
- Filterable by: date range, transaction status
- Transactions are **append-only** — no updates or deletes allowed
- Each transaction has a double-entry ledger: one `Debit` entry (source) + one `Credit` entry (destination)

## API

```
GET /api/v1/transactions?walletId={id}&page=1&pageSize=20&from=2024-01-01&to=2024-12-31
```

Response includes: `transactionId`, `sourceWalletId`, `destinationWalletId`, `amount`, `currency`, `status`, `createdAt`, `completedAt`.

## Implementation

- Separate **Read Model** — optimized query, no domain objects loaded
- Uses `AsNoTracking()` and projects directly to DTOs
- `ITransactionRepository` has NO `Update` or `Delete` methods by design

## Modules Involved

- `Transactions` — primary module
- `Transactions.Infrastructure` — query handler implementation

## Key Decisions

- The read side is a simple SQL query, not a CQRS event-sourced projection — the `transactions` table is the read model
- Authorization: users can only query wallets they own (`OwnerId == currentUserId` check in query handler)
