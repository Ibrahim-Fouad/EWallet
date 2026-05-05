# Feature: Real-Time Notifications

## Business Rules

- The recipient of a transfer receives a real-time push notification via SignalR when funds arrive
- Notification payload: `amount`, `currency`, `completedAt`
- Notification is triggered by `TransferCompletedEvent` via MassTransit

## Architecture

```
Transactions module
  → publishes TransferCompletedEvent (MassTransit + RabbitMQ)
Notifications module
  → TransferCompletedConsumer receives event
  → resolves destination wallet owner via IWalletLookupService
  → pushes to SignalR group "user-{ownerId}"
Client
  → listens on /hubs/notifications
  → receives "TransferReceived" message
```

## SignalR Hub

- Hub: `NotificationsHub` at `/hubs/notifications`
- Requires JWT authentication (`[Authorize]`)
- On connect: client is added to group `user-{userId}`
- Server-side push uses `IHubContext<NotificationsHub>` (not the Hub class itself)

## Hangfire Daily Reconciliation Job

- Runs daily at 2 AM UTC
- Validates: sum of all non-system wallet balances = net credit - net debit across all transaction entries
- Logs a Serilog `Warning` if any discrepancy is found
- Job name: `daily-reconciliation`

## Modules Involved

- `Notifications` — primary module
- `Transactions` — publishes the event
- `Wallets` — `IWalletLookupService` resolves destination wallet owner

## Key Decisions

- SignalR groups keyed by user ID (not wallet ID) — a user with multiple wallets receives notifications on one connection
- `IHubContext<NotificationsHub>` is injected into `NotificationService` for server-initiated pushes
- Hangfire dashboard at `/hangfire` is restricted to admin/internal access only
