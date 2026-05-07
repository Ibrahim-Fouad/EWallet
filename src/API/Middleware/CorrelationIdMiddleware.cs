using EWallet.BuildingBlocks.Application.Abstractions;
using Serilog.Context;

namespace EWallet.API.Middleware;

internal sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context, ICorrelationIdAccessor accessor)
    {
        var correlationId = ExtractOrGenerate(context.Request.Headers);

        accessor.Set(correlationId);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId.ToString();
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    private static Guid ExtractOrGenerate(IHeaderDictionary headers)
    {
        if (headers.TryGetValue(HeaderName, out var value) &&
            Guid.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return Guid.CreateVersion7();
    }
}
