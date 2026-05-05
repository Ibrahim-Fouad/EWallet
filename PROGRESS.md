# E-Wallet Implementation Progress

## Status: Steps 1–9 complete, Step 10 skipped — all planned steps done

---

## Completed Steps

### Step 1 — Solution Structure ✅

- `EWallet.sln` / `EWallet.slnx` created
- All 22 projects scaffolded and added to solution:
  - `src/API`, `src/Gateway`, `src/AppHost`, `src/ServiceDefaults`, `src/BuildingBlocks`
  - `src/Modules/{Identity,Wallets,Transactions,Notifications}/{Domain,Application,Infrastructure,API}`
  - `tests/EWallet.Tests.Integration`, `tests/EWallet.Tests.Unit`
- `Directory.Build.props` — net10.0, nullable, implicit usings, TreatWarningsAsErrors
- `Directory.Packages.props` — Central Package Management with all pinned versions
  - MediatR **12.5.0** (last Apache 2.0 — pinned, do NOT upgrade)
  - MassTransit **8.4.0** (last Apache 2.0 — pinned, do NOT upgrade)
- All project references wired per the dependency graph
- Gateway `Program.cs` + `appsettings.json` with YARP routes configured
- ServiceDefaults `Extensions.cs` with OpenTelemetry, health checks, service discovery
- **Build: 0 warnings, 0 errors**

### Step 2 — Docs Folder ✅

- `CLAUDE.md` at repo root (auto-loaded by Claude Code)
- `docs/features/wallet-management.md`
- `docs/features/money-transfer.md`
- `docs/features/idempotency.md`
- `docs/features/distributed-locking.md`
- `docs/features/transaction-history.md`
- `docs/features/real-time-notifications.md`
- `docs/architecture/patterns.md`
- `docs/architecture/decisions.md` (9 ADRs)

### Step 3 — Aspire Wiring ✅

- `src/AppHost/Program.cs` — orchestrates SQL Server, Redis, RabbitMQ, API, Gateway
- Uses `Aspire.AppHost.Sdk` NuGet SDK (workload deprecated in .NET 10)
- `.WaitFor()` on all resource dependencies

### Step 4 — BuildingBlocks ✅

- `Common/Error.cs` — `Error` record with factory methods
- `Common/Result.cs` — `Result` / `Result<T>` (no exceptions for business errors)
- `Common/PagedResult.cs`
- `Common/Constants/SystemConstants.cs` — hardcoded system GUIDs + welcome bonus amount
- `Domain/Abstractions/Entity.cs` — base entity with structural equality
- `Domain/Abstractions/AggregateRoot.cs` — raises/clears domain events
- `Domain/Abstractions/IDomainEvent.cs` — extends `INotification` for MediatR
- `Domain/Abstractions/ICommand.cs`, `IQuery.cs`, `ICommandHandler.cs`, `IQueryHandler.cs`, `IUnitOfWork.cs`
- `Domain/Primitives/ValueObject.cs`
- `Application/Behaviors/ValidationBehavior.cs`
- `Application/Behaviors/LoggingBehavior.cs`
- `Infrastructure/Contracts/IWalletLookupService.cs` + `WalletInfo` record
- `Infrastructure/Contracts/TransferCompletedEvent.cs` — cross-module MassTransit message contract
- **Build: 0 warnings, 0 errors**

### Step 5 — Identity Module ✅

- `Identity.Domain/Entities/ApplicationUser.cs` — extends `IdentityUser<Guid>`, adds `IsSystem`, `CreatedAt`
- `Identity.Application/Commands/Register/` — command, handler, validator
- `Identity.Application/Abstractions/IIdentityService.cs`
- `Identity.Application/DependencyInjection.cs`
- `Identity.Infrastructure/Persistence/IdentityDbContext.cs` — schema `identity`, `UseOpenIddict()`
- `Identity.Infrastructure/Persistence/Configurations/ApplicationUserConfiguration.cs` — seeds system user
- `Identity.Infrastructure/Services/IdentityService.cs`
- `Identity.Infrastructure/OpenIddict/OpenIddictWorker.cs` — seeds OAuth2 client on startup
- `Identity.Infrastructure/DependencyInjection.cs` — OpenIddict password flow, refresh tokens
- `Identity.API/Endpoints/IdentityEndpoints.cs` — `POST /api/v1/identity/register`
- **Build: 0 warnings, 0 errors**

### Step 6 — Wallets Module ✅

- `Wallets.Domain/Enums/Currency.cs` — EGP, USD
- `Wallets.Domain/Entities/Wallet.cs` — AggregateRoot with `Create()`, `Deposit()`, `Debit()`, `Credit()`; `RowVersion` for optimistic concurrency
- `Wallets.Domain/Events/WalletCreatedEvent.cs`, `FundsDepositedEvent.cs`
- `Wallets.Domain/Errors/WalletErrors.cs`
- `Wallets.Domain/Repositories/IWalletRepository.cs`
- `Wallets.Application/Commands/CreateWallet/` — command, handler (enforces max 3 wallets), validator
- `Wallets.Application/Commands/Deposit/` — command, handler
- `Wallets.Application/Queries/GetWalletById/` — query, handler
- `Wallets.Application/DTOs/WalletDto.cs`
- `Wallets.Application/EventHandlers/WalletCreatedEventHandler.cs` — applies welcome bonus (10 units from system wallet)
- `Wallets.Application/DependencyInjection.cs`
- `Wallets.Infrastructure/Persistence/WalletsDbContext.cs` — schema `wallets`, implements `IUnitOfWork`
- `Wallets.Infrastructure/Persistence/Configurations/WalletConfiguration.cs` — `decimal(18,4)`, `IsRowVersion()`, unique index on PhoneNumber, seeds system wallets
- `Wallets.Infrastructure/Persistence/Repositories/WalletRepository.cs`
- `Wallets.Infrastructure/Services/WalletLookupService.cs` — implements `IWalletLookupService`
- `Wallets.Infrastructure/DependencyInjection.cs`
- `Wallets.API/Endpoints/WalletEndpoints.cs` — create, get, deposit endpoints
- API `Program.cs` updated to register both Identity and Wallets modules
- **Build: 0 warnings, 0 errors**

---

### Step 7 — Transactions Module ✅

- `Transactions.Domain/Entities/Transaction.cs` — AggregateRoot with idempotency key, entries list, Complete()/Fail()
- `Transactions.Domain/Events/TransferInitiatedEvent.cs`
- `Transactions.Domain/Repositories/ITransactionRepository.cs`
- `Transactions.Application/Abstractions/IDistributedLockService.cs`
- `Transactions.Application/Abstractions/IIdempotencyService.cs`
- `Transactions.Application/Abstractions/ITransactionUnitOfWork.cs` — extends IUnitOfWork for keyed resolution
- `Transactions.Application/Commands/Transfer/TransferCommand.cs` + `TransferResponse`
- `Transactions.Application/Commands/Transfer/TransferCommandHandler.cs` — idempotency → validation → locks (alpha order) → debit/credit → TransactionScope save → Complete → dispatch events → cache
- `Transactions.Application/Commands/Transfer/TransferCommandValidator.cs`
- `Transactions.Application/Queries/GetTransactionHistory/` — paginated query + handler
- `Transactions.Application/DTOs/TransactionDto.cs`
- `Transactions.Application/DependencyInjection.cs`
- `Transactions.Infrastructure/Persistence/TransactionsDbContext.cs` — schema `transactions`, implements ITransactionUnitOfWork
- `Transactions.Infrastructure/Persistence/Configurations/TransactionConfiguration.cs`
- `Transactions.Infrastructure/Persistence/Configurations/TransactionEntryConfiguration.cs`
- `Transactions.Infrastructure/Persistence/Repositories/TransactionRepository.cs`
- `Transactions.Infrastructure/Locking/RedisDistributedLockService.cs` — SET NX EX with Lua CAS release script
- `Transactions.Infrastructure/Caching/RedisIdempotencyService.cs` — 24h TTL, System.Text.Json
- `Transactions.Infrastructure/DependencyInjection.cs`
- `Transactions.API/Endpoints/TransactionEndpoints.cs` — POST /transfer (rate limited) + GET history
- `Wallets.Infrastructure/DependencyInjection.cs` — added missing `IUnitOfWork → WalletsDbContext` registration
- `src/API/Program.cs` — sliding window rate limiter (10/min), `AddTransactionsModule`, `MapTransactionEndpoints`
- **Build: 0 warnings, 0 errors**

---

## Remaining Steps

### Step 8 — MassTransit + RabbitMQ ✅

- `BuildingBlocks/Application/Abstractions/IEventBus.cs` — abstraction over IPublishEndpoint
- `Transactions.Infrastructure/Services/MassTransitEventBus.cs` — implementation
- **Saga messages** (9 records in `Transactions.Application/Sagas/`):
  - `TransferRequestedMessage`, `DebitWalletCommand`, `CreditWalletCommand`, `ReverseDebitCommand`
  - `WalletDebitedEvent`, `WalletCreditedEvent`, `DebitFailedEvent`, `CreditFailedEvent`, `DebitReversedEvent`
- **`TransferSagaStateMachine`** — Initial → Debiting → Crediting → Completed | Failed | Compensating → Failed
  - `TransferSagaState.CorrelationId = TransactionId`
  - `TransferSagaStateConfiguration` (IEntityTypeConfiguration, `transactions.transfer_sagas` table)
- **Activities** (implement `IStateMachineActivity<TransferSagaState>`):
  - `CompleteTransactionActivity` — marks Transaction.Complete() in same DbContext transaction
  - `FailTransactionOnDebitActivity` — marks Transaction.Fail(reason)
  - `FailTransactionOnCompensationActivity` — marks Transaction.Fail after debit reversal
- **Consumers**: `DebitSourceWalletConsumer`, `CreditDestinationWalletConsumer`, `ReverseDebitConsumer`
- `TransactionsDbContext` updated: `AddInboxStateEntity()` + `AddOutboxMessageEntity()` + `AddOutboxStateEntity()` + `DbSet<TransferSagaState>`
- `Transaction.Fail(string? reason)` + `FailureReason` property added to domain entity + EF config
- `TransferCommandHandler` refactored: validate → DB idempotency fallback → create Pending TX → save → publish via IEventBus → cache pending response → return
- `GetTransactionByIdQuery` + handler + `GET /api/v1/transactions/{id}` endpoint (poll for final status)
- `Program.cs` — `AddMassTransit` with `SetKebabCaseEndpointNameFormatter`, saga + consumers registered, `AddEntityFrameworkOutbox<TransactionsDbContext>`, `AddConfigureEndpointsCallback` (applies EF outbox to all endpoints), `UseMessageRetry` 1s/5s/10s, `cfg.Host(rabbitmq)`
- **Build: 0 warnings, 0 errors**

### Step 9 — Notifications Module ✅

- `Notifications.Application/Abstractions/INotificationService.cs` — `SendTransferReceivedAsync(recipientUserId, transactionId, amount, currency)`
- `Notifications.Infrastructure/Hubs/NotificationsHub.cs` — `[Authorize]` SignalR hub; groups keyed by `Context.UserIdentifier` (JWT sub)
- `Notifications.Infrastructure/Services/NotificationService.cs` — pushes `TransferReceived` event to SignalR group
- `Notifications.Infrastructure/Consumers/TransferCompletedConsumer.cs` — MassTransit consumer; resolves recipient OwnerId via `IWalletLookupService`, then calls `INotificationService`
- `Notifications.Infrastructure/Jobs/ReconciliationJob.cs` — Hangfire daily job at 02:00 UTC; queries wallet balances + completed transaction volume via raw SQL; logs warning on negative balances
- `Notifications.Infrastructure/HangfireAuthorizationFilter.cs` — `IDashboardAuthorizationFilter` requiring authenticated user
- `Notifications.Infrastructure/DependencyInjection.cs` — `AddSignalR()`, Hangfire with schema `hangfire`, `UseHangfireDashboard("/hangfire")`, registers reconciliation recurring job
- `Notifications.API/Endpoints/NotificationsEndpoints.cs` — `MapHub<NotificationsHub>("/hubs/notifications")`
- `Notifications.Infrastructure.csproj` — added `Microsoft.Data.SqlClient` 5.2.2
- `Directory.Packages.props` — added `Microsoft.Data.SqlClient` version pin
- `src/API/Program.cs` — `AddNotificationsModule`, `MapNotificationsEndpoints`, `UseNotificationsModule`
- `MassTransitExtensions.cs` — `AddConsumer<TransferCompletedConsumer>()`
- **Build: 0 warnings, 0 errors**

### Step 10 — Tests ⏸ SKIPPED (deferred)

- `tests/EWallet.Tests.Integration/Fixtures/AppFixture.cs` — WebApplicationFactory + Testcontainers
- `tests/EWallet.Tests.Integration/Fixtures/DatabaseFixture.cs` — SQL Server via `Testcontainers.MsSql`
- Critical test scenarios:
  - Idempotency: same key twice → balance debited once
  - Concurrent transfers: two simultaneous transfers exceeding balance → only one succeeds
  - Welcome bonus: create wallet → balance = 10
  - Self-transfer → validation error
- `tests/EWallet.Tests.Unit/Domain/WalletTests.cs` — pure domain logic, no infrastructure

---

## EF Core Migrations (not yet run — run after all modules are wired)

```bash
# From D:\Personal\E.Wallet

# Identity
dotnet ef migrations add InitialCreate --project src/Modules/Identity/Identity.Infrastructure --startup-project src/API --context IdentityDbContext --output-dir Persistence/Migrations

# Wallets
dotnet ef migrations add InitialCreate --project src/Modules/Wallets/Wallets.Infrastructure --startup-project src/API --context WalletsDbContext --output-dir Persistence/Migrations

# Transactions (after Step 7 is complete)
dotnet ef migrations add InitialCreate --project src/Modules/Transactions/Transactions.Infrastructure --startup-project src/API --context TransactionsDbContext --output-dir Persistence/Migrations
```

---

## Key Gotchas to Remember

1. **MediatR 12.5.0** and **MassTransit 8.4.0** are pinned — never upgrade
2. Always `Guid.CreateVersion7()` — never `Guid.NewGuid()`
3. All money: `decimal(18,4)` — all timestamps: `DateTimeOffset` UTC
4. Redis lock order: alphabetical by `walletId.ToString()` to prevent deadlock
5. Idempotency check happens **before** acquiring any Redis locks
6. `DbUpdateConcurrencyException` is retriable — return `Result.Failure` with `ConcurrencyConflict` error
7. Transactions are append-only — `ITransactionRepository` has no Update/Delete
8. `TransactionEntry` records must be saved in the **same DB transaction** as wallet balance changes
9. `UseEntityFrameworkOutbox` must appear **before** `ConfigureEndpoints` in MassTransit RabbitMQ config
10. System wallets filtered from user-facing queries via `OwnerId != SystemConstants.SystemUserId`
