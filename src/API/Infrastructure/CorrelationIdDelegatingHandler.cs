namespace EWallet.API.Infrastructure;

internal sealed class CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    : DelegatingHandler
{
    private const string HeaderName = "X-Correlation-ID";

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContextAccessor.HttpContext?
            .Request.Headers[HeaderName]
            .FirstOrDefault();

        if (correlationId is not null)
            request.Headers.TryAddWithoutValidation(HeaderName, correlationId);

        return base.SendAsync(request, cancellationToken);
    }
}
