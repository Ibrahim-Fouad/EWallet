using EWallet.BuildingBlocks.Application.Abstractions;
using MassTransit;
using Serilog.Context;
using LogContext = Serilog.Context.LogContext;

namespace EWallet.API.Infrastructure;

public sealed class CorrelationIdConsumeFilter<T>(ICorrelationIdAccessor accessor)
    : IFilter<ConsumeContext<T>>
    where T : class
{
    private const string HeaderKey = "X-Correlation-ID";

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var id = context.Headers.TryGetHeader(HeaderKey, out var raw)
            && raw is string s && Guid.TryParse(s, out var parsed)
            ? parsed
            : Guid.CreateVersion7();

        accessor.Set(id);

        using (LogContext.PushProperty("CorrelationId", id))
        {
            await next.Send(context);
        }
    }

    public void Probe(ProbeContext context) =>
        context.CreateFilterScope("correlationId-consume");
}
