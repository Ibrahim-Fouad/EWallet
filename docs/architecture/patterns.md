# Architecture Patterns

## CQRS

All modules implement CQRS via MediatR 12.5.0. Commands mutate state and return `Result`. Queries read state and return `Result<T>`. Neither throws exceptions for business errors.

- Commands implement `ICommand` / `ICommand<T>` → handled by `ICommandHandler`
- Queries implement `IQuery<T>` → handled by `IQueryHandler`
- Both markers live in `BuildingBlocks/Application/Abstractions/`

## Domain Events (In-Process)

Domain events implement `IDomainEvent` (which extends `MediatR.INotification`). They are raised inside aggregate root methods via `RaiseDomainEvent()` and dispatched by `IUnitOfWork.DispatchDomainEventsAsync()` after `SaveChangesAsync` succeeds.

Used for same-module side effects (e.g., `WalletCreatedEvent` → welcome bonus).

## Domain Events (Cross-Module)

Cross-module events are published via MassTransit 8.4.0 + RabbitMQ. The event is published in the command handler after the DB transaction commits (via MassTransit Outbox pattern).

Used for: `TransferCompletedEvent` (Transactions → Notifications).

## Saga (Orchestration)

The Transfer use case is implemented as a Saga orchestrated within `TransferCommandHandler` (Steps 7). In Step 8, it is upgraded to a MassTransit State Machine (`TransferSagaStateMachine`) for resilience across process restarts.

States: `Initial → Debiting → Crediting → Completed | Failed | Compensating → Failed`

## Idempotency

The Transfer endpoint accepts a client-generated `IdempotencyKey` (UUID). The result is cached in Redis for 24 hours. Duplicate requests return the cached result immediately before any processing.

## Distributed Locking

Redis `SET key value NX EX` locks wallet balances before mutation. Locks are acquired in alphabetical order by wallet ID to prevent deadlock.

## Optimistic Concurrency

EF Core `RowVersion` (SQL Server `rowversion`) on the Wallet entity. If two concurrent writes target the same wallet, EF Core throws `DbUpdateConcurrencyException`, which is returned to the client as a retriable error.

## Outbox Pattern

MassTransit built-in Entity Framework Outbox (`UseEntityFrameworkOutbox<DbContext>`). Ensures messages are only published to RabbitMQ after the DB transaction commits — no dual-write problem.

## Repository Pattern

Each aggregate root has its own repository interface in the Domain layer and implementation in the Infrastructure layer. Repositories expose only the methods needed — no generic CRUD.

## Result Pattern

No exceptions for business errors. All command handlers and domain methods return `Result` or `Result<T>`. The global exception handler middleware catches only truly unexpected exceptions and returns 500.

## Audit Trail

Shadow properties or a separate audit table track `CreatedAt`, `ModifiedAt`, `CreatedBy` on all mutable entities. Transactions are append-only (no modification audit needed for them).
