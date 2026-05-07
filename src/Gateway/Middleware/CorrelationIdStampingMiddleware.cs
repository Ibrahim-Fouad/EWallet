namespace EWallet.Gateway.Middleware;

internal sealed class CorrelationIdStampingMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey(HeaderName))
            context.Request.Headers[HeaderName] = Guid.CreateVersion7().ToString();

        await next(context);
    }
}
