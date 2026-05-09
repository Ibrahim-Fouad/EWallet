using EWallet.Modules.Transactions.Application.Abstractions;
using StackExchange.Redis;

namespace EWallet.Modules.Transactions.Infrastructure.Locking;

internal sealed class RedisDistributedLockService(IConnectionMultiplexer redis) : IDistributedLockService
{
    public async Task<IAsyncDisposable?> TryAcquireAsync(
        string resource,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var token = Guid.CreateVersion7().ToString();

        var acquired = await db.StringSetAsync(resource, token, expiry, When.NotExists);
        if (!acquired)
            return null;

        return new RedisLock(db, resource, token);
    }

    private sealed class RedisLock(IDatabase db, string resource, string token) : IAsyncDisposable
    {
        private static readonly string ReleaseScript =
            "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

        public async ValueTask DisposeAsync()
        {
            await db.ScriptEvaluateAsync(ReleaseScript, [(RedisKey)resource], [(RedisValue)token]);
        }
    }
}
