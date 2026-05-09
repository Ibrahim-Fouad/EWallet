using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Wallets.Application;
using EWallet.Modules.Wallets.Domain.Repositories;
using EWallet.Modules.Wallets.Infrastructure.Persistence;
using EWallet.Modules.Wallets.Infrastructure.Persistence.Repositories;
using EWallet.Modules.Wallets.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EWallet.Modules.Wallets.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWalletsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddWalletsApplication();

        services.AddDbContext<WalletsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("sqlserver")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<WalletsDbContext>());
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IWalletLookupService, WalletLookupService>();

        return services;
    }

    public static async Task MigrateWalletsDatabaseAsync(IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WalletsDbContext>();
        await db.Database.MigrateAsync();
    }
}
