using EWallet.BuildingBlocks.Application.Abstractions;
using MassTransit;

namespace EWallet.API.Infrastructure;

public sealed class CorrelationIdSendFilter<T>(ICorrelationIdAccessor accessor)
    : IFilter<SendContext<T>>
    where T : class
{
    private const string HeaderKey = "X-Correlation-ID";

    public Task Send(SendContext<T> context, IPipe<SendContext<T>> next)
    {
        try
        {
            context.Headers.Set(HeaderKey, accessor.CorrelationId.ToString());
        }
        catch (InvalidOperationException)
        {
            context.Headers.Set(HeaderKey, Guid.CreateVersion7().ToString());
        }

        return next.Send(context);
    }

    public void Probe(ProbeContext context) =>
        context.CreateFilterScope("correlationId-send");
}
