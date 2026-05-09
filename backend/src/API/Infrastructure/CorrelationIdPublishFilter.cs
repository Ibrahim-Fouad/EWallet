using EWallet.BuildingBlocks.Application.Abstractions;
using MassTransit;

namespace EWallet.API.Infrastructure;

public sealed class CorrelationIdPublishFilter<T>(ICorrelationIdAccessor accessor)
    : IFilter<PublishContext<T>>
    where T : class
{
    private const string HeaderKey = "X-Correlation-ID";

    public Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
    {
        try
        {
            context.Headers.Set(HeaderKey, accessor.CorrelationId.ToString());
        }
        catch (InvalidOperationException)
        {
            // Outbox delivery service runs with a root-level scope where the accessor
            // was never initialized — generate a fresh ID so headers are always present.
            context.Headers.Set(HeaderKey, Guid.CreateVersion7().ToString());
        }

        return next.Send(context);
    }

    public void Probe(ProbeContext context) =>
        context.CreateFilterScope("correlationId-publish");
}
