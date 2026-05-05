# Architecture Decisions

## ADR-001: MediatR pinned to 12.5.0

**Decision:** Use MediatR 12.5.0 exactly. Never upgrade.

**Reason:** MediatR v13+ switched to a commercial license in July 2025. 12.5.0 is the last Apache 2.0 version.

**Impact:** No access to features introduced in v13+. If features are needed, evaluate alternatives (e.g., Mediator by martinothamar).

---

## ADR-002: MassTransit pinned to 8.4.0

**Decision:** Use MassTransit 8.4.0 exactly. Never upgrade.

**Reason:** MassTransit v9+ is expected to switch to a commercial license (Q1 2026). 8.4.0 is the last Apache 2.0 version.

**Impact:** No access to v9+ features. If needed, evaluate alternatives.

---

## ADR-003: Always use Guid.CreateVersion7()

**Decision:** Use `Guid.CreateVersion7()` for all new entity IDs. Never use `Guid.NewGuid()`.

**Reason:** Version 7 GUIDs are time-ordered (UUIDv7), which means sequential DB inserts without index fragmentation. Standard `Guid.NewGuid()` (UUIDv4) is random and causes significant B-tree fragmentation in SQL Server.

---

## ADR-004: decimal(18,4) for all money

**Decision:** All monetary amounts stored as `decimal(18,4)` in SQL Server. Never `float` or `double`.

**Reason:** Floating-point types cannot represent decimal fractions exactly (e.g., 0.1 + 0.2 ≠ 0.3). Financial calculations require exact decimal arithmetic.

---

## ADR-005: DateTimeOffset (not DateTime) for all timestamps

**Decision:** Use `DateTimeOffset` everywhere. Store in UTC.

**Reason:** `DateTime` has an ambiguous `Kind` property and can silently lose timezone information. `DateTimeOffset` is unambiguous and correct for distributed systems with multiple timezones.

---

## ADR-006: Modular Monolith over Microservices

**Decision:** Single deployable unit with strongly bounded modules.

**Reason:** Avoids distributed systems complexity (network partitions, distributed transactions) for an early-stage project while maintaining module isolation that allows future extraction to microservices if needed.

---

## ADR-007: YARP as public API Gateway

**Decision:** All public traffic enters via Gateway. The `src/API` host is internal only.

**Reason:** Centralizes rate limiting, TLS termination, and routing. In production, only Gateway is exposed publicly; API is on a private network.

---

## ADR-008: OpenIddict for OAuth2/OIDC

**Decision:** Use OpenIddict instead of IdentityServer/Duende.

**Reason:** OpenIddict is free and open-source. Duende IdentityServer requires a commercial license for production use. OpenIddict integrates natively with ASP.NET Core Identity and EF Core.

---

## ADR-009: Separate DbContext per module

**Decision:** Each module has its own `DbContext` and migrations, pointing to the same SQL Server database but using separate schemas.

**Reason:** Maintains module isolation at the persistence layer. Schemas: `identity`, `wallets`, `transactions`, `hangfire`.
