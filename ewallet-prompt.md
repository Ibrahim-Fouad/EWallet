# E-Wallet System — Project Prompt for Claude Code

## Role & Context

You are an expert Senior .NET Backend Developer helping me build a production-grade **E-Wallet System** as a side project. The goal is to learn and apply advanced backend patterns while keeping the business logic straightforward.

---

## Project Overview

Build a **Modular Monolith** E-Wallet system using **.NET 10**, **Clean Architecture**, and **Domain-Driven Design (DDD)** principles. The system allows users to create wallets, deposit funds, transfer money between wallets, and view transaction history — with real-time notifications and full auditability.

---

## Tech Stack (Non-Negotiable)

| Layer | Technology |
|---|---|
| Runtime | .NET 10 |
| Architecture | Modular Monolith + Clean Architecture |
| API Gateway | YARP (Yet Another Reverse Proxy) |
| Database | SQL Server 2022 (via EF Core) |
| Caching & Locking | Redis (StackExchange.Redis) |
| Real-time | SignalR |
| Background Jobs | Hangfire |
| In-process Messaging | MediatR v12.5.0 (CQRS + Domain Events) |
| Async Message Bus | MassTransit v8.4.0 + RabbitMQ (cross-module events + future scaling) |
| Orchestration (Local Dev) | .NET Aspire (dashboard + service discovery + observability) |
| Containerization (Production) | Docker + Docker Compose |
| API | ASP.NET Core Web API |
| Authentication | OpenIddict (OAuth2 + OpenID Connect) + ASP.NET Core Identity + JWT Bearer Tokens |
| Logging | Serilog (structured logging) |
| Testing | xUnit + Moq + Testcontainers |

> ⚠️ **Library Licensing Notice — CRITICAL:**
> - `MediatR` — pin to **v12.5.0** exactly. This is the last version under Apache 2.0. v13.0+ is commercial (since July 2025). Do NOT upgrade.
> - `MassTransit` — pin to **v8.4.0** exactly. This is the last open-source Apache 2.0 version. v9 is commercial (expected Q1 2026). Do NOT upgrade.
> - `OpenIddict` — free Apache 2.0. No version pinning required; use latest stable.
> - When adding any NuGet package related to MassTransit, always specify `Version="8.4.0"` explicitly.
> - When adding any NuGet package related to MediatR, always specify `Version="12.5.0"` explicitly.

> 📐 **MediatR vs MassTransit — When to Use Each:**
> - Use `MediatR` for **in-process** communication: commands, queries, and domain events within the same module.
> - Use `MassTransit + RabbitMQ` for **cross-module** async events (e.g., Transaction completed → Notification module reacts) and for anything that needs to survive process restart or scale horizontally.

> 🔀 **API Gateway — YARP:**
> - YARP (Yet Another Reverse Proxy) is a Microsoft-built reverse proxy library for ASP.NET Core.
> - It runs as a separate ASP.NET Core project (`Gateway/`) and sits in front of the `API` host.
> - Responsibilities: request routing, SSL termination, rate limiting, and forwarding `Authorization` headers to the API.
> - In local dev, Aspire orchestrates the Gateway project alongside the API.
> - In production, the Gateway container is the single public-facing entry point; the API container is internal only.
> - Do NOT duplicate authentication logic in the Gateway — JWT validation stays in the API host. The Gateway only forwards the `Authorization` header.

> 🔐 **Authentication — OpenIddict + ASP.NET Core Identity:**
> - `OpenIddict` runs embedded inside the `Identity Module` as a NuGet package — no external server or container needed.
> - It provides a fully standard `OAuth2` + `OpenID Connect` server including token issuance, refresh token rotation, revocation, and the `/.well-known/openid-configuration` discovery endpoint.
> - Enabled flows: `Resource Owner Password` (first-party clients) and `Client Credentials` (future service-to-service). `Authorization Code + PKCE` can be added later for third-party clients.
> - All tokens are stored in SQL Server via `OpenIddict` EF Core stores under the `identity` schema.
> - Use **ASP.NET Core Identity** (`Microsoft.AspNetCore.Identity`) for user management — password hashing, user store, sign-in manager, role manager. Do NOT re-implement these from scratch.
> - `OpenIddict` sits on top of `ASP.NET Core Identity` and delegates user validation to it.
> - Do NOT use `Duende IdentityServer` or `Keycloak` — `OpenIddict` is the chosen embedded solution.

---

## Architecture: Modular Monolith

The solution is split into **self-contained modules**. Each module has its own:
- Domain layer
- Application layer (CQRS with MediatR)
- Infrastructure layer
- Own DbContext (shared SQL Server instance, separate schemas)

### Modules:

```
src/
├── Gateway/               ← YARP API Gateway (public entry point)
├── Modules/
│   ├── Wallets/           ← Core wallet management
│   ├── Transactions/      ← Transfer + deposit logic
│   ├── Notifications/     ← SignalR real-time alerts
│   └── Identity/          ← JWT auth + user management
├── API/                   ← ASP.NET Core host (thin layer, internal only)
├── BuildingBlocks/        ← Shared kernel (base classes, interfaces, events)
├── AppHost/               ← .NET Aspire orchestration project (local dev entry point)
├── ServiceDefaults/       ← .NET Aspire shared defaults (telemetry, health checks, resilience)
└── docker-compose.yml     ← used for production deployment only
```

---

## Core Features (In Priority Order)

### 1. Identity Module
- Register / Login via `OpenIddict` OAuth2 endpoints (`/connect/token`)
- `OAuth2` access token + refresh token with automatic rotation
- `OpenID Connect` discovery endpoint (`/.well-known/openid-configuration`)
- Each user has **one row** in the `users` table
- A user can own **up to 3 wallets** — each wallet has its own `PhoneNumber`
- `PhoneNumber` is a `unique` field across the system — used as the public-facing wallet lookup key for transfers
- `Refresh token` strategy: on every refresh, old token is deleted and a new one is issued (rotation without reuse)

### 2. Wallet Module
- A user can own **up to 3 wallets** — enforced at creation time
- Each wallet has its own `PhoneNumber` as a unique identifier
- Each new wallet receives an automatic **welcome bonus of 10** (EGP or USD depending on currency) — recorded as a normal `transaction` from a system wallet
- Get wallet balance
- Deposit funds — any user can deposit into their own wallet
- All transfers are single-currency only — no cross-currency transfers supported
- Wallet has: `Id`, `OwnerId`, `PhoneNumber`, `Balance`, `Currency`, `CreatedAt`, `IsActive`
- A **system wallet** is seeded on first migration — it is the source of all welcome bonus transactions. It has a fixed well-known `Id` (hardcoded `GUID` constant) and is owned by a **system user** also seeded alongside it

### 3. Transaction Module (Most Complex)
- **Transfer** between two wallets using recipient's `PhoneNumber`
- Transfer is initiated using the recipient's `PhoneNumber` — the `Transactions Module` resolves it to an internal `wallet ID` via the `Wallets Module`
- **Self-transfer is forbidden** — if the recipient's `PhoneNumber` belongs to the sender, the request is rejected with a validation error
- All transfers are **single-currency** — source and destination wallets must share the same currency
- Enforce **Idempotency** via `IdempotencyKey` (UUID sent by client)
- Use **Saga Pattern** (orchestrated via MediatR pipeline):
  - Step 1: Debit source wallet
  - Step 2: Credit destination wallet
  - Step 3: Publish domain event
  - Compensate on failure (reverse debit)
- Use **Redis Distributed Lock** to prevent race conditions on wallet balance
- Use **Optimistic Concurrency** (`RowVersion` / `rowversion` in SQL Server via EF Core)
- All transactions are **append-only** (never deleted or updated)
- Full **Audit Trail** for every state change

### 4. Notifications Module
- SignalR hub: notify receiver in real-time when a transfer arrives
- Hangfire background job: daily reconciliation report (sum of all balances = sum of all transaction net)

### 5. Reporting (Simple)
- GET transaction history (paginated, filterable) — user sees all transactions on their wallet (both incoming and outgoing)
- Admin reporting (statistics, aggregates) — deferred to a future phase
- Uses separate **Read Model** (CQRS read side, optimized query)

---

## Key Patterns to Implement

| Pattern | Where |
|---|---|
| CQRS | All modules via MediatR |
| Domain Events (in-process) | MediatR Notifications within same module |
| Domain Events (cross-module) | MassTransit v8.4.0 + RabbitMQ |
| Saga (Orchestration) | Transfer use case via MassTransit Saga State Machine |
| Idempotency | Transfer endpoint |
| Distributed Locking | Redis lock on wallet before debit |
| Optimistic Concurrency | EF Core concurrency token on Wallet |
| Audit Trail | Shadow properties or separate audit table |
| Outbox Pattern | MassTransit built-in Outbox (reliable event publishing via SQL Server) |
| Repository Pattern | Per aggregate root |
| Result Pattern | No exceptions for business errors (use `Result<T>`) |

---

## Database Design (High Level)

### Schema: `wallets`
```sql
wallets(id, owner_id, phone_number, balance, currency, row_version, is_active, created_at)
-- phone_number: unique index — one wallet per phone number
```

### Seed Data (applied on first migration)
```sql
-- system user (owns the system wallet)
users(id: '00000000-0000-0000-0000-000000000001', email: 'system@ewallet.internal', is_system: true)

-- system wallets — one per supported currency
wallets(id: '00000000-0000-0000-0000-000000000001', owner_id: '...system user...', phone_number: 'SYSTEM-EGP', currency: 'EGP', balance: 1000000, is_active: true)
wallets(id: '00000000-0000-0000-0000-000000000002', owner_id: '...system user...', phone_number: 'SYSTEM-USD', currency: 'USD', balance: 1000000, is_active: true)
```
- System wallet `Id` is a **hardcoded constant** referenced in code — never changes
- System user and wallets are **never exposed** via any API endpoint
- The system wallet `balance` is not subject to reconciliation checks

### Schema: `transactions`
```sql
transactions(id, idempotency_key, source_wallet_id, destination_wallet_id, amount, currency, status, created_at, completed_at, failure_reason)
transaction_entries(id, transaction_id, wallet_id, entry_type [debit/credit], amount, created_at)
```

### Schema: `outbox`
```sql
outbox_messages(id, type, payload, created_at, processed_at, error)
```

---

## .NET Aspire Setup (Local Development)

```csharp
// AppHost/Program.cs — Aspire orchestration entry point
var builder = DistributedApplication.CreateBuilder(args);

var sqlserver = builder.AddSqlServer("sqlserver");
var redis     = builder.AddRedis("redis");
var rabbitmq  = builder.AddRabbitMQ("rabbitmq");

var api = builder.AddProject<Projects.API>("api")
                 .WithReference(sqlserver)
                 .WithReference(redis)
                 .WithReference(rabbitmq);

builder.AddProject<Projects.Gateway>("gateway")
       .WithReference(api);

builder.Build().Run();
```

- `AppHost` هو نقطة البداية للـ `local development` بدل `docker-compose`
- `ServiceDefaults` بيضيف `OpenTelemetry`, `health checks`, و`resilience` تلقائياً لكل `service`
- الـ `Aspire Dashboard` بيظهر `logs`, `traces`, و`metrics` لكل الـ `services` في مكان واحد
- الـ `connection strings` بتتمرر تلقائياً من `Aspire` للـ `API` — مفيش `hardcoded` values
- الـ `Gateway` بياخد الـ `API` كـ `reference` عشان `Aspire` يمرر الـ `URL` بتاعه تلقائياً

## Docker Setup (Production Only)

```yaml
# docker-compose.yml — للـ production deployment فقط
services:
  gateway:    # YARP — public entry point (ports 80/443 exposed)
  api:        # ASP.NET Core app (internal only, no public ports)
  sqlserver:  # SQL Server 2022
  redis:      # Redis 7
  rabbitmq:   # RabbitMQ 3 (management UI on port 15672)
```

All configuration via environment variables. No hardcoded connection strings.

---

## Non-Functional Requirements

- All money amounts stored as `decimal(18,4)` — never `float` or `double`
- All timestamps in UTC
- Always use `DateTimeOffset` instead of `DateTime` everywhere in the codebase
- API versioning from day one (`/api/v1/...`)
- Global exception handler middleware
- Structured logging with correlation ID per request
- All endpoints require authentication except Register (`/api/v1/identity/register`) and Token (`/connect/token`)
- Rate limiting on Transfer endpoint (max 10 transfers/minute per user)
- Always use `Guid.CreateVersion7()` when generating a new `GUID` anywhere in the codebase — never use `Guid.NewGuid()`

---

## Documentation Structure (docs/ folder)

Every feature has its own markdown file under `docs/features/`. This folder is **independent of the codebase** and can be reused across projects.

### Folder Layout:

```
docs/
├── CLAUDE.md                          ← index file, read by Claude Code automatically at session start
├── features/
│   ├── wallet-management.md
│   ├── money-transfer.md
│   ├── idempotency.md
│   ├── distributed-locking.md
│   ├── transaction-history.md
│   └── real-time-notifications.md
└── architecture/
    ├── patterns.md
    └── decisions.md
```

### CLAUDE.md (root-level index — kept short intentionally):

```markdown
# E-Wallet — Claude Context Index

## Features
- [Wallet Management](./docs/features/wallet-management.md)
- [Money Transfer](./docs/features/money-transfer.md)
- [Idempotency](./docs/features/idempotency.md)
- [Distributed Locking](./docs/features/distributed-locking.md)
- [Transaction History](./docs/features/transaction-history.md)
- [Real-time Notifications](./docs/features/real-time-notifications.md)

## Architecture
- [Patterns & Conventions](./docs/architecture/patterns.md)
- [Key Decisions](./docs/architecture/decisions.md)

## Rules
- Always read the relevant feature file before implementing or modifying any feature.
- Never modify a feature doc without confirming with the user first.
```

### Template for each feature file:

```markdown
# Feature Name

## Business Rules
- Rule 1
- Rule 2

## Edge Cases
- Edge case 1
- Edge case 2

## Modules Involved
- Module A, Module B

## Key Decisions
- Decision and reason
```

### Agent Instructions (CRITICAL):

- **Before implementing any feature** — read its file under `docs/features/` first.
- **After implementing any feature** — update its file with any new decisions or edge cases discovered.
- **When a new edge case or business rule is found during implementation** — add it to the relevant feature file immediately, before continuing.
- **Keep `CLAUDE.md` short** — it is an index only, never add content directly to it.
- **Feature files are project-agnostic** — write them in a way that can be understood outside this codebase.

---

## Development Approach

1. **Start with the solution structure** — scaffold all projects including `Gateway`, `AppHost` and `ServiceDefaults`
2. **Scaffold docs folder** — create `CLAUDE.md` and all feature files with empty templates
3. **Aspire wiring** — get `AppHost` running with `Gateway`, `SQL Server`, `Redis`, `RabbitMQ` all orchestrated
4. **BuildingBlocks first** — base entities, Result pattern, domain events interfaces
5. **Identity module** — auth working end-to-end via Aspire
6. **Wallet module** — balance management with concurrency handling
7. **Transaction module** — full Saga (MassTransit) with Redis locking and Idempotency
8. **MassTransit + RabbitMQ wiring** — cross-module events between Transaction and Notification
9. **Notifications module** — SignalR hub triggered by MassTransit consumers
10. **Tests** — integration tests using Testcontainers for each module

---

## What I Want From You

- Guide me **step by step**, one task at a time
- Write **production-quality code** — no shortcuts, no TODO comments left behind
- Explain **why** before **how** for any complex pattern
- When there are multiple approaches, present the trade-offs briefly then recommend one
- Always show the **full file** when creating or editing — no partial snippets
- After each step, tell me exactly what to do next

---

## Starting Point

Begin by scaffolding the complete **solution structure** with all projects, folders, and references — nothing implemented yet, just the skeleton. Use `dotnet CLI` commands I can run one by one.
