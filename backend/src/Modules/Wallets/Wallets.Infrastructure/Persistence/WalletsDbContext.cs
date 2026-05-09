using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Domain.Abstractions;
using EWallet.Modules.Wallets.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EWallet.Modules.Wallets.Infrastructure.Persistence;

public sealed class WalletsDbContext(DbContextOptions<WalletsDbContext> options, IMediator mediator)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Wallet> Wallets => Set<Wallet>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("wallets");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WalletsDbContext).Assembly);
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
