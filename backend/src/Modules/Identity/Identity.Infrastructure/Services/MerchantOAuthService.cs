using EWallet.Modules.Identity.Application.Abstractions;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace EWallet.Modules.Identity.Infrastructure.Services;

internal sealed class MerchantOAuthService(
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictTokenManager tokenManager,
    ILogger<MerchantOAuthService> logger)
    : IMerchantOAuthService
{
    public async Task<string> CreateClientAsync(
        Guid merchantId,
        string clientSecret,
        CancellationToken cancellationToken = default)
    {
        var clientId = $"merchant-{merchantId}";

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            ClientType = ClientTypes.Confidential,
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.ClientCredentials,
            }
        };

        var existing = await applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (existing is null)
            await applicationManager.CreateAsync(descriptor, cancellationToken);
        else
            await applicationManager.UpdateAsync(existing, descriptor, cancellationToken);

        logger.LogInformation("Created OAuth client {ClientId} for merchant {MerchantId}", clientId, merchantId);
        return clientId;
    }

    public async Task DisableClientAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        var clientId = $"merchant-{merchantId}";
        var client = await applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (client is null)
        {
            logger.LogWarning("DisableClientAsync: client {ClientId} not found", clientId);
            return;
        }

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientType = ClientTypes.Confidential,
        };

        await applicationManager.UpdateAsync(client, descriptor, cancellationToken);
        logger.LogInformation("Disabled OAuth client {ClientId}", clientId);
    }

    public async Task RevokeAllTokensAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        var clientId = $"merchant-{merchantId}";
        var client = await applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (client is null)
        {
            logger.LogWarning("RevokeAllTokensAsync: client {ClientId} not found", clientId);
            return;
        }

        var clientDbId = await applicationManager.GetIdAsync(client, cancellationToken);
        await foreach (var token in tokenManager.FindByApplicationIdAsync(clientDbId!, cancellationToken))
        {
            await tokenManager.TryRevokeAsync(token, cancellationToken);
        }

        logger.LogInformation("Revoked all tokens for OAuth client {ClientId}", clientId);
    }
}
