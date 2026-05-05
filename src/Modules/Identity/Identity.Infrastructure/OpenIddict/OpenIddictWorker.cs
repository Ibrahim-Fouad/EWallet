using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace EWallet.Modules.Identity.Infrastructure.OpenIddict;

/// <summary>
/// Seeds / migrates the OAuth2 client application record for the Authorization Code + PKCE flow.
/// <para>
/// Uses <c>UpdateAsync</c> when the record already exists so that existing deployments
/// transition from the old password-flow permissions to PKCE without manual DB intervention.
/// </para>
/// <para>
/// Ordering guarantee: <c>DatabaseMigrationService</c> runs as an <c>IHostedLifecycleService</c>
/// (StartingAsync phase) which completes before all <c>IHostedService.StartAsync</c> calls,
/// so migrations are applied before this worker touches the OpenIddict schema.
/// </para>
/// </summary>
public sealed class OpenIddictWorker(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var manager = scope.ServiceProvider
            .GetRequiredService<IOpenIddictApplicationManager>();

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId    = "ewallet-client",
            ClientType  = ClientTypes.Public,
            DisplayName = "E-Wallet Client",
            RedirectUris =
            {
                new Uri("https://localhost:7000/scalar/"),
            },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Email,
                Permissions.Prefixes.Scope + "wallet",  // custom scope — gates phone_number claim
            }
        };

        var client = await manager.FindByClientIdAsync("ewallet-client", cancellationToken);
        if (client is null)
            await manager.CreateAsync(descriptor, cancellationToken);
        else
            await manager.UpdateAsync(client, descriptor, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
