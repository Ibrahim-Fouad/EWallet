using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.Modules.Transactions.Application;
using EWallet.Modules.Transactions.Application.Abstractions;
using EWallet.Modules.Transactions.Domain.Repositories;
using EWallet.Modules.Transactions.Infrastructure.Caching;
using EWallet.Modules.Transactions.Infrastructure.Locking;
using EWallet.Modules.Transactions.Infrastructure.Persistence;
using EWallet.Modules.Transactions.Infrastructure.Persistence.Repositories;
using EWallet.Modules.Transactions.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace EWallet.Modules.Transactions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTransactionsApplication();

        services.AddDbContext<TransactionsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("sqlserver")));

        services.AddScoped<ITransactionUnitOfWork>(sp =>
            sp.GetRequiredService<TransactionsDbContext>());

        services.AddScoped<ITransactionRepository, TransactionRepository>();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("redis")!));

        services.AddScoped<IDistributedLockService, RedisDistributedLockService>();
        services.AddScoped<IIdempotencyService, RedisIdempotencyService>();

        // IEventBus wraps MassTransit IPublishEndpoint.
        // In HTTP-handler context: uses AddEntityFrameworkOutbox<TransactionsDbContext>()
        // so that PublishAsync() + SaveChangesAsync() are atomic.
        services.AddScoped<IEventBus, MassTransitEventBus>();

        return services;
    }

    public static async Task MigrateTransactionsDatabaseAsync(IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();
        await db.Database.EnsureCreatedAsync();
        await db.Database.MigrateAsync();
    }
}
