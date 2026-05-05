# E-Wallet — Claude Code Index

## Project Summary

Production-grade e-wallet modular monolith. .NET 10, Clean Architecture, CQRS, Saga, Distributed Locking, Idempotency, SignalR, Hangfire.

## Critical Version Pins (Licensing)

| Package | Version | Reason |
|---|---|---|
| MediatR | 12.5.0 | Last Apache 2.0 — v13+ is commercial |
| MassTransit | 8.4.0 | Last Apache 2.0 — v9+ is commercial |

**Never upgrade these without explicit decision.**

## Solution Structure

```
src/
├── Gateway/          YARP reverse proxy (public entry point)
├── API/              ASP.NET Core host (internal, thin layer)
├── AppHost/          .NET Aspire orchestration (local dev)
├── ServiceDefaults/  Shared Aspire defaults (OTel, health, service discovery)
├── BuildingBlocks/   Shared kernel (Result, AggregateRoot, behaviors)
└── Modules/
    ├── Identity/     Auth — OpenIddict + ASP.NET Core Identity
    ├── Wallets/      Wallet management + balance
    ├── Transactions/ Transfer saga + idempotency + distributed locking
    └── Notifications/ SignalR + Hangfire
```

Each module: `{Module}.Domain` → `{Module}.Application` → `{Module}.Infrastructure` + `{Module}.API` (classlib of endpoints).

## Key Rules

- Always use `Guid.CreateVersion7()` — never `Guid.NewGuid()`
- All money: `decimal(18,4)` — never `float`/`double`
- All timestamps: `DateTimeOffset` UTC
- No exceptions for business errors — use `Result<T>` from BuildingBlocks
- All endpoints require auth except `POST /api/v1/identity/register` and `POST /connect/token`
- API versioning: `/api/v1/...` from day one

## Feature Documentation

See `docs/features/` for business rules on each feature area.
See `docs/architecture/` for pattern and ADR documentation.

## Running Locally

```bash
dotnet run --project src/AppHost
```

Aspire Dashboard auto-opens. Gateway listens publicly; API is internal only.

## Migrations

```bash
# Run per module:
dotnet ef migrations add <Name> --project src/Modules/<Module>/<Module>.Infrastructure --startup-project src/API --context <Module>DbContext
dotnet ef database update --project src/Modules/<Module>/<Module>.Infrastructure --startup-project src/API --context <Module>DbContext
```
