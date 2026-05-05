using EWallet.Modules.Identity.Application;
using EWallet.Modules.Identity.Domain.Entities;
using EWallet.Modules.Identity.Infrastructure.OpenIddict;
using EWallet.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace EWallet.Modules.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddIdentityApplication();

        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("sqlserver"));
            options.UseOpenIddict();
        });

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit           = true;
            options.Password.RequireLowercase       = true;
            options.Password.RequireUppercase       = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength         = 8;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<IdentityDbContext>()
        .AddDefaultTokenProviders()
        .AddSignInManager()     // required by Identity Razor Pages (not included in AddIdentityCore by default)
        .AddDefaultUI();        // registers embedded /Account/* Razor Pages

        // Cookie used ONLY by the OpenIddict authorization server login flow
        // (/connect/authorize → /Account/Login or /Identity/Account/Register).
        // MUST be IdentityConstants.ApplicationScheme — SignInManager.PasswordSignInAsync
        // and SignInManager.SignInAsync both write to this scheme.
        // The API itself continues to use JWT Bearer tokens exclusively.
        services.AddAuthentication()
            .AddCookie(IdentityConstants.ApplicationScheme, options =>
            {
                options.LoginPath         = "/Account/Login";
                options.LogoutPath        = "/Account/Logout";
                options.ExpireTimeSpan    = TimeSpan.FromMinutes(15);
                options.SlidingExpiration = false;
            })
            // The built-in Identity Login Razor Page calls SignOutAsync(IdentityConstants.ExternalScheme)
            // on every GET to clear leftover external-login cookies.  We do not use external logins,
            // but the scheme must still be registered or ASP.NET Core throws an InvalidOperationException.
            .AddCookie(IdentityConstants.ExternalScheme);

        services.AddOpenIddict()
            .AddCore(options =>
                options.UseEntityFrameworkCore()
                       .UseDbContext<IdentityDbContext>())
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("/connect/authorize");
                options.SetTokenEndpointUris("/connect/token");

                options.AllowAuthorizationCodeFlow()
                       .RequireProofKeyForCodeExchange();
                options.AllowRefreshTokenFlow();

                // Declare the scopes the server accepts — OpenIddict rejects any scope
                // not listed here, even well-known OIDC scopes like profile/email.
                options.RegisterScopes(
                    OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Scopes.OfflineAccess,
                    "wallet");  // custom scope — gates phone_number claim

                options.SetRefreshTokenLifetime(TimeSpan.FromDays(30));

                options.AddDevelopmentEncryptionCertificate()
                        .AddDevelopmentSigningCertificate();
                
                options.DisableAccessTokenEncryption();

                options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough();
                //.EnableTokenEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        services.AddHostedService<OpenIddictWorker>();

        return services;
    }
}
