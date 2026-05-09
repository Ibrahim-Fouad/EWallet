using EWallet.BuildingBlocks.Domain.Abstractions;
using EWallet.Modules.Transactions.Application.Abstractions;
using EWallet.Modules.Transactions.Domain.Entities;
using EWallet.Modules.Transactions.Infrastructure.Sagas;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EWallet.Modules.Transactions.Infrastructure.Persistence;

public sealed class TransactionsDbContext(DbContextOptions<TransactionsDbContext> options, IMediator mediator)
    : DbContext(options), ITransactionUnitOfWork
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionEntry> TransactionEntries => Set<TransactionEntry>();
    public DbSet<TransferSagaState> TransferSagas => Set<TransferSagaState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("transactions");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransactionsDbContext).Assembly);

        // MassTransit EF Core outbox tables (schema defaults to "transactions" above)
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }

    public async Task DispatchDomainEventsAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
        aggregates.ForEach(a => a.ClearDomainEvents());

        foreach (var @event in events)
            await mediator.Publish(@event, cancellationToken);
    }
}
