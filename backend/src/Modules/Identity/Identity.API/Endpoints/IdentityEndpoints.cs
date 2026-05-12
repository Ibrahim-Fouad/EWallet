using System.Security.Claims;
using EWallet.Modules.Identity.Domain.Entities;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace EWallet.Modules.Identity.API.Endpoints;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        // OpenIddict validates the incoming request params (client_id, redirect_uri,
        // response_type, PKCE code_challenge) then passes control here via passthrough.
        app.MapGet("/connect/authorize", async (HttpContext context) =>
        {
            var request = context.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException("OpenIddict server request is unavailable.");

            var result = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);

            if (!result.Succeeded)
            {
                return Results.Challenge(
                    new AuthenticationProperties
                    {
                        RedirectUri = context.Request.PathBase + context.Request.Path
                            + QueryString.Create(
                                context.Request.Query.Select(kv =>
                                    new KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>(
                                        kv.Key, kv.Value))),
                    },
                    [IdentityConstants.ApplicationScheme]);
            }

            var cookiePrincipal = result.Principal!;
            var sub = cookiePrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? throw new InvalidOperationException("User principal has no NameIdentifier claim.");

            var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var appUser = await userManager.FindByIdAsync(sub)
                          ?? throw new InvalidOperationException($"User '{sub}' not found.");

            var identity = new ClaimsIdentity(
                authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                nameType: OpenIddictConstants.Claims.Name,
                roleType: OpenIddictConstants.Claims.Role);

            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject,       sub));
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Email,         appUser.Email        ?? string.Empty));
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.EmailVerified, appUser.EmailConfirmed ? "true" : "false"));
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name,          appUser.FullName));
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.PhoneNumber,   appUser.PhoneNumber  ?? string.Empty));

            var roles = await userManager.GetRolesAsync(appUser);
            foreach (var role in roles)
                identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, role));

            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(request.GetScopes());

            foreach (var claim in principal.Claims)
                claim.SetDestinations(GetDestinations(claim, principal));

            return Results.SignIn(principal,
                authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        })
        .AllowAnonymous()
        .ExcludeFromDescription();

        // Token endpoint — handles Authorization Code, Refresh Token, and Client Credentials flows
        app.MapPost("/connect/token", async (HttpContext context) =>
        {
            var request = context.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException("OpenIddict server request is unavailable.");

            if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
            {
                // Exchange the authorization code / refresh token for the stored principal.
                var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                var principal = result.Principal
                    ?? throw new InvalidOperationException("The principal could not be retrieved from the token.");

                foreach (var claim in principal.Claims)
                    claim.SetDestinations(GetDestinations(claim, principal));

                return Results.SignIn(principal,
                    authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            if (request.IsClientCredentialsGrantType())
            {
                var applicationManager = context.RequestServices
                    .GetRequiredService<IOpenIddictApplicationManager>();

                var application = await applicationManager.FindByClientIdAsync(request.ClientId!)
                    ?? throw new InvalidOperationException($"Application '{request.ClientId}' not found.");

                var identity = new ClaimsIdentity(
                    authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    nameType: OpenIddictConstants.Claims.Name,
                    roleType: OpenIddictConstants.Claims.Role);

                identity.AddClaim(new Claim(
                    OpenIddictConstants.Claims.Subject, request.ClientId!));

                // Embed the merchant_id — extract the GUID from "merchant-{guid}"
                if (request.ClientId!.StartsWith("merchant-", StringComparison.OrdinalIgnoreCase))
                {
                    var merchantIdStr = request.ClientId["merchant-".Length..];
                    identity.AddClaim(new Claim("merchant_id", merchantIdStr)
                        .SetDestinations(OpenIddictConstants.Destinations.AccessToken));
                }

                var principal = new ClaimsPrincipal(identity);
                principal.SetScopes(request.GetScopes());

                foreach (var claim in principal.Claims)
                {
                    if (claim.Type == OpenIddictConstants.Claims.Subject)
                        claim.SetDestinations(
                            OpenIddictConstants.Destinations.AccessToken,
                            OpenIddictConstants.Destinations.IdentityToken);
                }

                return Results.SignIn(principal,
                    authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            return Results.BadRequest(new { error = "unsupported_grant_type" });
        })
        .AllowAnonymous()
        .ExcludeFromDescription();

        return app;
    }

    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Subject:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;

            case OpenIddictConstants.Claims.Email:
            case OpenIddictConstants.Claims.EmailVerified:
                if (principal.HasScope(OpenIddictConstants.Scopes.Email))
                {
                    yield return OpenIddictConstants.Destinations.AccessToken;
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                }
                yield break;

            case OpenIddictConstants.Claims.Name:
                if (principal.HasScope(OpenIddictConstants.Scopes.Profile))
                {
                    yield return OpenIddictConstants.Destinations.AccessToken;
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                }
                yield break;

            case OpenIddictConstants.Claims.PhoneNumber:
                if (principal.HasScope("wallet"))
                    yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;

            case OpenIddictConstants.Claims.Role:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;

            default:
                yield break;
        }
    }
}
