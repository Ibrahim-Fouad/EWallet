# Feature: Distributed Locking

## Business Rules

- A Redis distributed lock is acquired on each wallet before modifying its balance
- Lock key: `lock:wallet:{walletId}`
- Lock TTL: 30 seconds (prevents deadlock if process crashes holding the lock)
- Lock acquisition timeout: 5 seconds (returns `WalletLocked` error if exceeded)

## Implementation

- `IDistributedLockService.AcquireAsync(resource, expiry, ct)` returns an `IAsyncDisposable` lock handle
- Implementation uses Redis `SET key value NX EX` (atomic acquire + TTL) via Lua script
- Locks are always released in a `finally` block

## Deadlock Prevention

- When two wallets must be locked (transfer involves source AND destination), **always acquire in alphabetical order by `walletId.ToString()`**
- This ensures concurrent transfers between the same pair of wallets cannot deadlock each other

## Modules Involved

- `Transactions.Application` — `IDistributedLockService` interface
- `Transactions.Infrastructure` — `RedisDistributedLockService` implementation

## Key Decisions

- Redis distributed lock (not DB-level lock) — avoids holding DB connections
- Optimistic concurrency (`RowVersion`) on Wallet provides an additional safety net: even if two transfers somehow pass the Redis lock, the DB will catch the conflict
- The lock is scoped tightly — acquired just before balance mutation, released immediately after `SaveChanges`
