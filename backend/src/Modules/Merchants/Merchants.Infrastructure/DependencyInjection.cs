using EWallet.Modules.Merchants.Application;
using EWallet.Modules.Merchants.Application.Abstractions;
using EWallet.Modules.Merchants.Domain.Repositories;
using EWallet.Modules.Merchants.Infrastructure.Jobs;
using EWallet.Modules.Merchants.Infrastructure.Persistence;
using EWallet.Modules.Merchants.Infrastructure.Persistence.Repositories;
using EWallet.Modules.Merchants.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EWallet.Modules.Merchants.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMerchantsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMerchantsApplication();

        services.AddDbContext<MerchantsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("sqlserver")));

        services.AddScoped<IMerchantUnitOfWork>(sp =>
            sp.GetRequiredService<MerchantsDbContext>());

        services.AddScoped<IMerchantRepository, MerchantRepository>();
        services.AddScoped<IPaymentRequestRepository, PaymentRequestRepository>();

        services.AddScoped<IWebhookSigner, WebhookSignerService>();

        services.AddScoped<ExpirePaymentRequestJob>();
        services.AddScoped<DispatchWebhookJob>();

        services.AddHttpClient("webhook");

        services.AddDataProtection();

        return services;
    }

    public static async Task MigrateMerchantsDatabaseAsync(IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MerchantsDbContext>();
        await db.Database.EnsureCreatedAsync();
        await db.Database.MigrateAsync();
    }
}
