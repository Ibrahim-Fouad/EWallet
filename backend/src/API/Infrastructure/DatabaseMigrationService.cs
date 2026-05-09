using EWallet.Modules.Identity.Infrastructure.Persistence;
using EWallet.Modules.Transactions.Infrastructure.Persistence;
using EWallet.Modules.Wallets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace EWallet.API.Infrastructure;

/// <summary>
/// Applies all pending EF Core migrations during the <see cref="IHostedLifecycleService.StartingAsync"/> phase.
///
/// The .NET runtime Host guarantees the following global phase order:
///   Phase 1 — StartingAsync : all IHostedLifecycleService implementations, in registration order
///   Phase 2 — StartAsync    : all IHostedService implementations, in registration order
///   Phase 3 — StartedAsync  : all IHostedLifecycleService implementations, in registration order
///
/// Because this service is registered FIRST in Program.cs, its StartingAsync is the very first
/// thing the host executes — before OpenIddictWorker.StartAsync, before MassTransit connects to
/// RabbitMQ, and before the outbox delivery service starts polling.
///
/// DI readiness: even though this service is registered before the module DI extensions, the
/// container is built once at builder.Build() after ALL registrations complete. Every DbContext
/// (IdentityDbContext, WalletsDbContext, TransactionsDbContext) is resolvable from the scope
/// created inside StartingAsync.
/// </summary>
internal sealed class DatabaseMigrationService(
    IServiceProvider serviceProvider,
    ILogger<DatabaseMigrationService> logger) : IHostedLifecycleService
{
    // ── Phase 1 — runs before ALL IHostedService.StartAsync calls ────────────

    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        try
        {
            // Order matters: Identity seeds the system user referenced by the
            // Wallets seed data (system wallet OwnerId).
            await MigrateAsync<IdentityDbContext>(sp, "identity", cancellationToken);
            await MigrateAsync<WalletsDbContext>(sp, "wallets", cancellationToken);
            await MigrateAsync<TransactionsDbContext>(sp, "transactions", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database migration failed — application cannot start");
            throw;
        }
    }

    // ── Remaining lifecycle methods — no-op pass-throughs ────────────────────

    public Task StartAsync(CancellationToken cancellationToken)    => Task.CompletedTask;
    public Task StartedAsync(CancellationToken cancellationToken)  => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken)     => Task.CompletedTask;
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StoppedAsync(CancellationToken cancellationToken)  => Task.CompletedTask;

    // ── Migration helper ──────────────────────────────────────────────────────

    private async Task MigrateAsync<TContext>(
        IServiceProvider sp,
        string schemaName,
        CancellationToken cancellationToken)
        where TContext : DbContext
    {
        var context = sp.GetRequiredService<TContext>();
        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pending.Count == 0)
        {
            logger.LogInformation(
                "Database [{Schema}] is up to date — no pending migrations",
                schemaName);
            return;
        }

        logger.LogInformation(
            "Database [{Schema}] applying {Count} pending migration(s): {Migrations}",
            schemaName,
            pending.Count,
            string.Join(", ", pending));

        await context.Database.MigrateAsync(cancellationToken);

        logger.LogInformation(
            "Database [{Schema}] migration completed successfully",
            schemaName);
    }
}
