using EWallet.API.Extensions;
using EWallet.API.Infrastructure;
using EWallet.API.Middleware;
using EWallet.BuildingBlocks.Application.Abstractions;
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

builder.Services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();
builder.Services.ConfigureHttpClientDefaults(http =>
    http.AddHttpMessageHandler<CorrelationIdDelegatingHandler>());

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
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();

// SignalR WebSocket connections cannot carry Authorization headers.
// Read access_token from the query string and inject it as a Bearer header
// so OpenIddict's validation handler can authenticate hub connections normally.
app.Use(async (context, next) =>
{
    var accessToken = context.Request.Query["access_token"].ToString();
    if (!string.IsNullOrEmpty(accessToken) &&
        context.Request.Path.StartsWithSegments("/hubs"))
    {
        context.Request.Headers.Authorization = $"Bearer {accessToken}";
    }
    await next(context);
});

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