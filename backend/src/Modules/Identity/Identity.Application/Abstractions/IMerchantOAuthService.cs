namespace EWallet.Modules.Identity.Application.Abstractions;

public interface IMerchantOAuthService
{
    Task<string> CreateClientAsync(Guid merchantId, string clientSecret, CancellationToken cancellationToken = default);
    Task DisableClientAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task RevokeAllTokensAsync(Guid merchantId, CancellationToken cancellationToken = default);
}
