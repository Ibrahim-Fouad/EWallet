using EWallet.API.Extensions;
using EWallet.API.Infrastructure;
using EWallet.Modules.Identity.API.Endpoints;
using EWallet.Modules.Identity.Infrastructure;
using EWallet.Modules.Notifications.API.Endpoints;
using EWallet.Modules.Notifications.Infrastructure;
using EWallet.Modules.Transactions.API.Endpoints;
using EWallet.Modules.Transactions.Infrastructure;
using EWallet.Modules.Wallets.API.Endpoints;
using EWallet.Modules.Wallets.Infrastructure;
using Serilog;
using EWallet.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, loggerConfig) =>
    loggerConfig
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.OpenTelemetry()
        .WriteTo.Console());

builder.AddServiceDefaults();

// ── Database migration service (must be first hosted service) ────────────────
// IHostedLifecycleService.StartingAsync runs as a global phase BEFORE all
// IHostedService.StartAsync calls, so migrations complete before OpenIddictWorker,
// MassTransit, and the outbox delivery service touch any schema.
builder.Services.AddHostedService<DatabaseMigrationService>();
// ─────────────────────────────────────────────────────────────────────────────

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddRazorPages(); // required by the Identity UI Razor Pages

builder.Services.AddOpenApiDocument();
builder.Services.AddApiRateLimiter();

builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddWalletsModule(builder.Configuration);
builder.Services.AddTransactionsModule(builder.Configuration);
builder.Services.AddNotificationsModule(builder.Configuration);

builder.Services.AddMassTransitWithRabbitMq(builder.Configuration);

var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapApiDocs();
app.MapDefaultEndpoints();
app.MapRazorPages(); // maps /Identity/Account/* routes
app.MapIdentityEndpoints();
app.MapWalletEndpoints();
app.MapTransactionEndpoints();
app.MapNotificationsEndpoints();
app.UseNotificationsModule();

app.Run();

public partial class Program
{
}