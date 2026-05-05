# E-Wallet System

A production-grade **Modular Monolith** e-wallet built with **.NET 10**, Clean Architecture, DDD, CQRS, and event-driven patterns. Designed to demonstrate real-world backend engineering — idempotency, distributed locking, sagas, real-time notifications, and full auditability.

---

## Features

| #   | Feature                                                                  | Status |
| --- | ------------------------------------------------------------------------ | ------ |
| 1   | User registration & OAuth2 login (OpenIddict)                            | ✅     |
| 2   | JWT Bearer access tokens + refresh token rotation                        | ✅     |
| 3   | Create up to 3 wallets per user (EGP / USD)                              | ✅     |
| 4   | Welcome bonus on wallet creation (10 units from system wallet)           | ✅     |
| 5   | Deposit funds into own wallet                                            | ✅     |
| 6   | Transfer money between wallets (by phone number)                         | ✅     |
| 7   | Idempotent transfers (UUID idempotency key)                              | ✅     |
| 8   | Redis distributed locking (prevents race conditions)                     | ✅     |
| 9   | Optimistic concurrency (SQL `rowversion`)                                | ✅     |
| 10  | Transfer Saga with compensation (debit → credit → rollback on failure)   | ✅     |
| 11  | Outbox pattern (reliable event publishing via SQL Server)                | ✅     |
| 12  | Real-time notifications via SignalR on transfer received                 | ✅     |
| 13  | Paginated transaction history                                            | ✅     |
| 14  | Daily reconciliation background job (Hangfire)                           | ✅     |
| 15  | Rate limiting on transfer endpoint (10/min per user)                     | ✅     |
| 16  | .NET Aspire local dev orchestration (dashboard, OTel, service discovery) | ✅     |
| 17  | YARP API Gateway (single public entry point)                             | ✅     |

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Internet / Clients                          │
└──────────────────────────────┬──────────────────────────────────────┘
                               │ HTTPS
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     Gateway (YARP Reverse Proxy)                    │
│  • Routes /api/v1/** → API                                          │
│  • Routes /connect/**  → API  (OAuth2 token endpoint)              │
│  • Forwards Authorization header — no JWT validation here           │
└──────────────────────────────┬──────────────────────────────────────┘
                               │ Internal HTTP
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│                       API Host (internal only)                      │
│  ASP.NET Core — thin host, registers all modules                   │
│                                                                     │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────────────────┐   │
│  │   Identity   │  │   Wallets    │  │     Transactions       │   │
│  │   Module     │  │   Module     │  │       Module           │   │
│  │              │  │              │  │                        │   │
│  │ OpenIddict   │  │ Wallet CRUD  │  │ Transfer Saga          │   │
│  │ ASP Identity │  │ Balance Mgmt │  │ Idempotency            │   │
│  │ JWT Issuance │  │ Welcome Bonus│  │ Distributed Lock       │   │
│  └──────┬───────┘  └──────┬───────┘  └───────────┬────────────┘   │
│         │                 │                       │                 │
│         └─────────────────┴───────────┬───────────┘                │
│                                       │                             │
│  ┌────────────────────────────────────▼───────────────────────┐    │
│  │                   BuildingBlocks (Shared Kernel)           │    │
│  │   Result<T> · AggregateRoot · CQRS interfaces · Behaviors  │    │
│  └────────────────────────────────────────────────────────────┘    │
│                                                                     │
│  ┌────────────────────────────────────────────────────────────┐    │
│  │               Notifications Module                         │    │
│  │   SignalR Hub · MassTransit Consumer · Hangfire Jobs       │    │
│  └────────────────────────────────────────────────────────────┘    │
└──────┬──────────────┬─────────────────────┬────────────────────────┘
       │              │                     │
       ▼              ▼                     ▼
  SQL Server       Redis               RabbitMQ
  (EF Core)    (Lock + Cache)        (MassTransit)
  3 schemas:
  identity
  wallets
  transactions
```

---

## High-Level Design

### Modular Monolith

Each module is an independent vertical slice with its own:

- **Domain** — entities, value objects, domain events, repository interfaces
- **Application** — CQRS commands/queries (MediatR), validators, event handlers
- **Infrastructure** — EF Core DbContext, repositories, external service clients
- **API** — minimal API endpoint definitions (class library, mapped at host level)

Modules share a **single SQL Server instance** but use **separate schemas** — `identity`, `wallets`, `transactions`, `hangfire`. Cross-module communication uses **MassTransit + RabbitMQ** (never direct in-process calls between modules).

### Request Pipeline

```
Client Request
    → Gateway (YARP)
    → API Host
    → Auth Middleware (JWT validation)
    → Rate Limiter
    → Minimal API Endpoint
    → MediatR Pipeline
        → LoggingBehavior
        → ValidationBehavior (FluentValidation)
        → Command/Query Handler
    → Response
```

### Money Flow

```
Deposit:   External → User Wallet   (no saga, direct)
Transfer:  User Wallet → Saga → Destination Wallet
Bonus:     System Wallet → New Wallet  (on WalletCreated domain event)
```

---

## Workflow Diagrams

### Transfer Saga (Happy Path)

```
Client                API                TransferSaga            RabbitMQ
  │                    │                      │                      │
  │  POST /transfer    │                      │                      │
  │ + IdempotencyKey   │                      │                      │
  │───────────────────►│                      │                      │
  │                    │ Check idempotency    │                      │
  │                    │ (Redis / DB)         │                      │
  │                    │                      │                      │
  │                    │ Create TX (Pending)  │                      │
  │                    │ Save to DB           │                      │
  │                    │                      │                      │
  │                    │ Publish TransferRequestedMessage            │
  │                    │─────────────────────────────────────────────►
  │                    │                      │                      │
  │  202 Accepted      │                      │  DebitWalletCommand  │
  │◄───────────────────│                      │◄─────────────────────│
  │                    │                      │                      │
  │                    │                      │ Acquire Redis lock   │
  │                    │                      │ (alphabetical order) │
  │                    │                      │                      │
  │                    │                      │ Debit source wallet  │
  │                    │                      │ Check RowVersion     │
  │                    │                      │                      │
  │                    │                      │ WalletDebitedEvent ──►
  │                    │                      │                      │
  │                    │                      │  CreditWalletCommand │
  │                    │                      │◄─────────────────────│
  │                    │                      │                      │
  │                    │                      │ Credit dest wallet   │
  │                    │                      │ Release Redis locks  │
  │                    │                      │                      │
  │                    │                      │ WalletCreditedEvent ─►
  │                    │                      │                      │
  │                    │                      │ Mark TX Complete     │
  │                    │                      │ Publish TransferCompleted
  │                    │                      │─────────────────────►│
  │                    │                      │                      │
  │  GET /transactions/{id}                   │  SignalR Push to     │
  │───────────────────►│                      │  recipient (real-time)
  │  200 Completed     │                      │                      │
  │◄───────────────────│                      │                      │
```

### Transfer Saga (Compensation — Credit Fails)

```
Saga State: Debiting → Crediting → FAILED → Compensating → FAILED

1. DebitSourceWallet     → OK  → state: Crediting
2. CreditDestWallet      → FAIL → state: Compensating
3. ReverseDebit          → OK  → Mark TX as Failed, release locks
   (system publishes ReverseDebitCommand → consumer reverses balance)
```

### Idempotency Check

```
Client sends same IdempotencyKey twice:

First request:
  → Key not found in Redis/DB
  → Process normally
  → Cache result in Redis (24h TTL)
  → Return 202

Second request (duplicate):
  → Key found in Redis
  → Return cached result immediately (no DB write, no locks)
  → Return 202 (same response)
```

### Wallet Creation with Welcome Bonus

```
POST /api/v1/wallets
    → CreateWalletCommand
    → Handler validates: user owns < 3 wallets
    → Wallet.Create() → raises WalletCreatedDomainEvent
    → SaveChanges()
    → MediatR dispatches WalletCreatedEventHandler (in-process)
    → Handler creates Transfer: SystemWallet → NewWallet (amount: 10)
    → Welcome balance applied in same DB transaction
```

---

## Tech Stack

| Concern                 | Technology                                      |
| ----------------------- | ----------------------------------------------- |
| Runtime                 | .NET 10                                         |
| API Framework           | ASP.NET Core Minimal APIs                       |
| API Gateway             | YARP (Yet Another Reverse Proxy)                |
| Auth                    | OpenIddict + ASP.NET Core Identity + JWT Bearer |
| ORM                     | EF Core 10 (SQL Server 2022)                    |
| In-process Messaging    | MediatR 12.5.0 (CQRS + domain events)           |
| Async Message Bus       | MassTransit 8.4.0 + RabbitMQ                    |
| Saga Orchestration      | MassTransit Saga State Machine                  |
| Caching & Locking       | Redis (StackExchange.Redis)                     |
| Real-time               | SignalR                                         |
| Background Jobs         | Hangfire                                        |
| Observability           | OpenTelemetry + .NET Aspire Dashboard           |
| Local Dev Orchestration | .NET Aspire                                     |
| Production Deployment   | Docker + Docker Compose                         |
| Logging                 | Serilog                                         |
| Validation              | FluentValidation                                |
| Testing                 | xUnit + Moq + Testcontainers                    |

> **License pins:** MediatR is pinned to 12.5.0 and MassTransit to 8.4.0 — both are the last Apache 2.0 releases. Do not upgrade.

---

## Solution Structure

```
EWallet.sln
├── src/
│   ├── Gateway/                        YARP reverse proxy (public entry point)
│   ├── API/                            ASP.NET Core host (internal only)
│   ├── AppHost/                        .NET Aspire orchestration (local dev)
│   ├── ServiceDefaults/                Shared OTel, health checks, resilience
│   ├── BuildingBlocks/
│   │   ├── Common/                     Result<T>, Error, PagedResult, Constants
│   │   ├── Domain/Abstractions/        Entity, AggregateRoot, IDomainEvent
│   │   ├── Domain/Primitives/          ValueObject
│   │   ├── Application/Behaviors/      ValidationBehavior, LoggingBehavior
│   │   └── Infrastructure/Contracts/   IWalletLookupService, TransferCompletedEvent
│   └── Modules/
│       ├── Identity/
│       │   ├── Identity.Domain/        ApplicationUser
│       │   ├── Identity.Application/   Register command, IIdentityService
│       │   ├── Identity.Infrastructure/ IdentityDbContext, OpenIddict wiring
│       │   └── Identity.API/           POST /api/v1/identity/register
│       ├── Wallets/
│       │   ├── Wallets.Domain/         Wallet aggregate, events, errors
│       │   ├── Wallets.Application/    CreateWallet, Deposit, GetWalletById
│       │   ├── Wallets.Infrastructure/ WalletsDbContext, WalletRepository
│       │   └── Wallets.API/            Wallet endpoints
│       ├── Transactions/
│       │   ├── Transactions.Domain/    Transaction aggregate, entries
│       │   ├── Transactions.Application/ Transfer command, Saga messages, history query
│       │   ├── Transactions.Infrastructure/ TransactionsDbContext, RedisLock, Sagas
│       │   └── Transactions.API/       POST /transfer, GET /transactions
│       └── Notifications/
│           ├── Notifications.Application/ INotificationService
│           ├── Notifications.Infrastructure/ SignalR hub, MassTransit consumer, Hangfire
│           └── Notifications.API/      SignalR hub mapping
└── tests/
    ├── EWallet.Tests.Integration/      Testcontainers-based integration tests
    └── EWallet.Tests.Unit/             Domain unit tests
```

---

## Database Schemas

```sql
-- schema: identity
users(id, email, phone_number, is_system, created_at, ...)
-- + OpenIddict tables: applications, authorizations, scopes, tokens

-- schema: wallets
wallets(id, owner_id, phone_number, balance decimal(18,4), currency,
        row_version, is_active, created_at)
-- unique index on phone_number

-- schema: transactions
transactions(id, idempotency_key, source_wallet_id, destination_wallet_id,
             amount decimal(18,4), currency, status, created_at,
             completed_at, failure_reason)
transaction_entries(id, transaction_id, wallet_id, entry_type, amount, created_at)
transfer_sagas(correlation_id, current_state, transaction_id, ...)
-- + MassTransit outbox tables

-- schema: hangfire  (managed by Hangfire)
```

**Seed data:** System user + two system wallets (EGP / USD) with 1,000,000 balance each, used exclusively for welcome bonus disbursements.

---

## API Endpoints

| Method | Path                            | Auth                      | Description                       |
| ------ | ------------------------------- | ------------------------- | --------------------------------- |
| POST   | `/api/v1/identity/register`     | No                        | Register new user                 |
| POST   | `/connect/token`                | No                        | OAuth2 token (password / refresh) |
| POST   | `/api/v1/wallets`               | Yes                       | Create wallet                     |
| GET    | `/api/v1/wallets/{id}`          | Yes                       | Get wallet by ID                  |
| POST   | `/api/v1/wallets/{id}/deposit`  | Yes                       | Deposit into wallet               |
| POST   | `/api/v1/transactions/transfer` | Yes (rate-limited 10/min) | Initiate transfer                 |
| GET    | `/api/v1/transactions/{id}`     | Yes                       | Get transaction status            |
| GET    | `/api/v1/transactions`          | Yes                       | Paginated transaction history     |
| WS     | `/hubs/notifications`           | Yes                       | SignalR real-time feed            |
| GET    | `/hangfire`                     | Yes                       | Hangfire dashboard                |

---

## Running Locally

**Prerequisites:** .NET 10 SDK, Docker Desktop

```bash
# Start all services (SQL Server, Redis, RabbitMQ, API, Gateway)
dotnet run --project src/AppHost
```

Aspire Dashboard opens automatically at `http://localhost:15888`.  
Gateway is the public entry point (default: `http://localhost:5000`).

### EF Core Migrations

```bash
# Identity
dotnet ef migrations add InitialCreate \
  --project src/Modules/Identity/Identity.Infrastructure \
  --startup-project src/API \
  --context IdentityDbContext \
  --output-dir Persistence/Migrations

# Wallets
dotnet ef migrations add InitialCreate \
  --project src/Modules/Wallets/Wallets.Infrastructure \
  --startup-project src/API \
  --context WalletsDbContext \
  --output-dir Persistence/Migrations

# Transactions
dotnet ef migrations add InitialCreate \
  --project src/Modules/Transactions/Transactions.Infrastructure \
  --startup-project src/API \
  --context TransactionsDbContext \
  --output-dir Persistence/Migrations
```

---

## Key Engineering Decisions

| Decision                  | Choice                         | Reason                                                   |
| ------------------------- | ------------------------------ | -------------------------------------------------------- |
| Monolith vs Microservices | Modular Monolith               | Simpler deployment, still module-isolated                |
| Auth server               | OpenIddict (embedded)          | No external server, Apache 2.0, standard OAuth2/OIDC     |
| Saga style                | MassTransit Saga State Machine | Durable state, automatic compensation routing            |
| Lock ordering             | Alphabetical by wallet ID      | Prevents deadlock on concurrent bi-directional transfers |
| Idempotency storage       | Redis (primary) + DB fallback  | Fast path avoids DB round-trip on duplicate              |
| GUID generation           | `Guid.CreateVersion7()` only   | Monotonically increasing — better index locality         |
| Money type                | `decimal(18,4)`                | Never float/double — avoids rounding errors              |
| Timestamps                | `DateTimeOffset` UTC           | Timezone-safe, SQL Server compatible                     |

---

## Architecture Decision Records

Full ADRs are documented in [`docs/architecture/decisions.md`](docs/architecture/decisions.md).

Key decisions:

- **ADR-001** — Modular Monolith over Microservices
- **ADR-002** — OpenIddict over Duende IdentityServer / Keycloak
- **ADR-003** — MassTransit Saga over MediatR-based saga
- **ADR-004** — Redis SET NX for distributed locking (Redlock not needed for single-node)
- **ADR-005** — EF Core Outbox (MassTransit built-in) over custom outbox table
