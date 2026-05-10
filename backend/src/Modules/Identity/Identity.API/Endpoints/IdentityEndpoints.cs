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

            // Read the Identity application cookie set by SignInManager after login or registration.
            // MUST use IdentityConstants.ApplicationScheme — the same scheme SignInManager writes to.
            var result = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);

            if (!result.Succeeded)
            {
                // No valid cookie — challenge redirects to /Account/Login?ReturnUrl=<this URL>.
                // After login (or registration), the user is sent back here with the cookie present.
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

            // Cookie is valid — resolve the full user from DB to access all profile properties.
            var cookiePrincipal = result.Principal!;
            var sub = cookiePrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? throw new InvalidOperationException("User principal has no NameIdentifier claim.");

            var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var appUser = await userManager.FindByIdAsync(sub)
                          ?? throw new InvalidOperationException($"User '{sub}' not found.");

            // Build the OpenIddict principal. nameType + roleType ensure OpenIddict maps
            // standard claim type constants to the correct JWT claim names.
            var identity = new ClaimsIdentity(
                authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                nameType: OpenIddictConstants.Claims.Name,
                roleType: OpenIddictConstants.Claims.Role);

            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject,       sub));
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Email,         appUser.Email        ?? string.Empty));
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.EmailVerified, appUser.EmailConfirmed ? "true" : "false"));
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name,          appUser.FullName));
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.PhoneNumber,   appUser.PhoneNumber  ?? string.Empty));

            var principal = new ClaimsPrincipal(identity);

            // SetScopes MUST be called before destinations are evaluated — GetDestinations
            // reads the granted scopes from the principal to decide which tokens each claim
            // should appear in.
            principal.SetScopes(request.GetScopes());

            foreach (var claim in principal.Claims)
                claim.SetDestinations(GetDestinations(claim, principal));

            // Signing in with the OpenIddict server scheme causes OpenIddict to generate
            // the authorization code and redirect the browser to redirect_uri?code=...
            return Results.SignIn(principal,
                authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        })
        .AllowAnonymous()
        .ExcludeFromDescription(); // IdP endpoint — not an API operation; keep OpenAPI clean

        return app;
    }

    /// <summary>
    /// Returns the token types a claim should be included in, based on which scopes were granted.
    /// OpenIddict silently drops any claim whose destination set is empty.
    /// </summary>
    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        switch (claim.Type)
        {
            // sub is always present in both token types.
            case OpenIddictConstants.Claims.Subject:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;

            // email + email_verified — only when the 'email' scope was granted.
            case OpenIddictConstants.Claims.Email:
            case OpenIddictConstants.Claims.EmailVerified:
                if (principal.HasScope(OpenIddictConstants.Scopes.Email))
                {
                    yield return OpenIddictConstants.Destinations.AccessToken;
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                }
                yield break;

            // name — only when the 'profile' scope was granted.
            case OpenIddictConstants.Claims.Name:
                if (principal.HasScope(OpenIddictConstants.Scopes.Profile))
                {
                    yield return OpenIddictConstants.Destinations.AccessToken;
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                }
                yield break;

            // phone_number — only when the custom 'wallet' scope was granted.
            case OpenIddictConstants.Claims.PhoneNumber:
                if (principal.HasScope("wallet"))
                    yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;

            default:
                yield break;
        }
    }
}
