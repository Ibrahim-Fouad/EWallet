using EWallet.BuildingBlocks.Application.Abstractions;
using MassTransit;

namespace EWallet.Modules.Transactions.Infrastructure.Services;

/// <summary>
/// Wraps MassTransit's IPublishEndpoint.
/// In non-consumer contexts (HTTP handlers), this uses the EF Core outbox when
/// AddEntityFrameworkOutbox&lt;TransactionsDbContext&gt;() is configured — messages are
/// staged in-memory and committed atomically with DbContext.SaveChangesAsync().
/// In consumer contexts, the receive-endpoint outbox middleware takes over.
/// </summary>
internal sealed class MassTransitEventBus(IPublishEndpoint publishEndpoint) : IEventBus
{
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
        => publishEndpoint.Publish(message, cancellationToken);
}
