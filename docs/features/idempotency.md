# Feature: Idempotency

## Business Rules

- The Transfer endpoint requires an `IdempotencyKey` (UUID) sent by the client
- If the same key is sent again, the original result is returned — no double-debit occurs
- Idempotency check happens **before** any Redis locks or DB operations
- Idempotency results are stored in Redis with a **24-hour TTL**

## Implementation

- `IdempotencyKey` is a `Guid` (preferably `Guid.CreateVersion7()` from client)
- Stored in Redis as: `idempotency:{key}` → serialized `Result<TransferDto>` JSON
- On hit: deserialize and return immediately
- On miss: execute transfer, then store result in Redis

## Modules Involved

- `Transactions` — primary module
- `Transactions.Infrastructure` — `RedisIdempotencyRepository` implementation

## Key Decisions

- Redis is preferred over DB for idempotency (fast, automatic TTL expiry)
- The idempotency cache is keyed per endpoint — a different endpoint could reuse the same key without conflict (key includes endpoint prefix)
- At the saga level (Step 8), `TransferSagaState.CorrelationId = IdempotencyKey` provides natural saga-level idempotency
