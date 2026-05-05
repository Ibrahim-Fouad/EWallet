namespace EWallet.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Abstraction over MassTransit's IPublishEndpoint.
/// Keeps the Application layer free of MassTransit references.
/// In production, the implementation uses the EF Core outbox so that
/// messages are published atomically with DbContext.SaveChangesAsync().
/// </summary>
public interface IEventBus
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class;
}
