using EWallet.Modules.Notifications.Application.Abstractions;
using EWallet.Modules.Notifications.Application.Queries.GetNotificationHistory;
using EWallet.Modules.Notifications.Infrastructure.Hubs;
using EWallet.Modules.Notifications.Infrastructure.Jobs;
using EWallet.Modules.Notifications.Infrastructure.Persistence;
using EWallet.Modules.Notifications.Infrastructure.Persistence.Repositories;
using EWallet.Modules.Notifications.Infrastructure.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EWallet.Modules.Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSignalR();

        services.AddDbContext<NotificationsDbContext>(opts =>
            opts.UseSqlServer(configuration.GetConnectionString("sqlserver")));

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationService, NotificationService>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GetNotificationHistoryQuery).Assembly));

        services.AddHangfire(cfg =>
            cfg.UseSqlServerStorage(
                configuration.GetConnectionString("sqlserver"),
                new SqlServerStorageOptions
                {
                    SchemaName = "hangfire",
                    PrepareSchemaIfNecessary = true
                }));

        services.AddHangfireServer();
        services.AddScoped<ReconciliationJob>();

        return services;
    }

    public static void UseNotificationsModule(this IApplicationBuilder app)
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new HangfireAuthorizationFilter()]
        });

        RecurringJob.AddOrUpdate<ReconciliationJob>(
            "reconciliation",
            job => job.RunAsync(),
            "0 2 * * *",
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
    }
}
