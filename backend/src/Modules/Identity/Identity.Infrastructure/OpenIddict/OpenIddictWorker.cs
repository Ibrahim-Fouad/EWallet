using EWallet.Modules.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace EWallet.Modules.Identity.Infrastructure.OpenIddict;

public sealed class OpenIddictWorker(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<OpenIddictWorker> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        await SeedOAuthClientAsync(sp, cancellationToken);
        await SeedAdminRoleAndUserAsync(sp, cancellationToken);
    }

    private static async Task SeedOAuthClientAsync(IServiceProvider sp, CancellationToken cancellationToken)
    {
        var manager = sp.GetRequiredService<IOpenIddictApplicationManager>();

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId    = "ewallet-client",
            ClientType  = ClientTypes.Public,
            DisplayName = "E-Wallet Client",
            RedirectUris =
            {
                new Uri("http://localhost:4200/auth/callback"),
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
                Permissions.Prefixes.Scope + "wallet",
            }
        };

        var client = await manager.FindByClientIdAsync("ewallet-client", cancellationToken);
        if (client is null)
            await manager.CreateAsync(descriptor, cancellationToken);
        else
            await manager.UpdateAsync(client, descriptor, cancellationToken);
    }

    private async Task SeedAdminRoleAndUserAsync(IServiceProvider sp, CancellationToken cancellationToken)
    {
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        const string adminRole = "Admin";

        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            var result = await roleManager.CreateAsync(new IdentityRole<Guid>(adminRole) { Id = Guid.CreateVersion7() });
            if (result.Succeeded)
                logger.LogInformation("Admin role created");
            else
                logger.LogWarning("Failed to create Admin role: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        var adminEmail    = configuration["Identity:AdminEmail"]    ?? "admin@ewallet.dev";
        var adminPassword = configuration["Identity:AdminPassword"] ?? "Admin@123!";

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id             = Guid.CreateVersion7(),
                Email          = adminEmail,
                UserName       = adminEmail,
                FullName       = "System Admin",
                PhoneNumber    = "SYSTEM-ADMIN",
                NationalId     = "ADMIN-00000000",
                EmailConfirmed = true,
                IsSystem       = true,
                CreatedAt      = DateTimeOffset.UtcNow
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (!result.Succeeded)
            {
                logger.LogWarning("Failed to create admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("Admin user {Email} created", adminEmail);
        }

        if (!await userManager.IsInRoleAsync(admin, adminRole))
        {
            await userManager.AddToRoleAsync(admin, adminRole);
            logger.LogInformation("Admin user {Email} assigned to Admin role", adminEmail);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
