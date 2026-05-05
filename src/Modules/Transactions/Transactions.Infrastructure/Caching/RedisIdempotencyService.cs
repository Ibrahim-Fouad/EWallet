using System.Text.Json;
using EWallet.Modules.Transactions.Application.Abstractions;
using EWallet.Modules.Transactions.Application.Commands.Transfer;
using StackExchange.Redis;

namespace EWallet.Modules.Transactions.Infrastructure.Caching;

internal sealed class RedisIdempotencyService(IConnectionMultiplexer redis) : IIdempotencyService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);
    private const string KeyPrefix = "idempotency:transfer:";

    public async Task<TransferResponse?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(KeyPrefix + key);
        if (value.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<TransferResponse>((string)value!);
    }

    public async Task SetAsync(string key, TransferResponse response, CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var json = JsonSerializer.Serialize(response);
        await db.StringSetAsync(KeyPrefix + key, json, Ttl);
    }
}
