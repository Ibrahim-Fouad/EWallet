using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace EWallet.API.Extensions;

internal static class OpenApiExtensions
{
    /// <summary>
    /// Registers the OpenAPI document generator with an OAuth2 Authorization Code + PKCE security scheme.
    /// </summary>
    internal static IServiceCollection AddOpenApiDocument(this IServiceCollection services)
    {
        // IHttpContextAccessor is needed so the document transformer can read the
        // current request's origin and inject it as the OpenAPI servers[0].url.
        // Without this, Scalar cannot resolve the relative /connect/authorize and
        // /connect/token URLs and shows an immediate error when "Authorize" is clicked.
        services.AddHttpContextAccessor();

        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, context, _) =>
            {
                // Inject the request origin so Scalar can resolve relative OAuth2 endpoint URIs.
                // Without a servers entry, Scalar 2.x cannot build the absolute authorization
                // URL needed to open the OAuth2 popup, causing an immediate client-side error.
                var httpContext = context.ApplicationServices
                    .GetService<IHttpContextAccessor>()?.HttpContext;

                if (httpContext is not null)
                {
                    var req = httpContext.Request;
                    document.Servers = [new OpenApiServer { Url = $"{req.Scheme}://{req.Host}" }];
                }

                document.Info = new OpenApiInfo
                {
                    Title       = "E-Wallet API",
                    Version     = "v1",
                    Description = "Production-grade modular-monolith e-wallet — wallet management, money transfer, and real-time notifications.",
                };

                // OAuth2 Authorization Code + PKCE scheme — OpenIddict endpoints.
                // Scalar reads the authorizationCode flow and handles PKCE natively.
                var oauthScheme = new OpenApiSecurityScheme
                {
                    Type        = SecuritySchemeType.OAuth2,
                    Description = "Authenticate via Authorization Code + PKCE. Click Authorize to open the Identity login page.",
                    Flows       = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"{document.Servers[0].Url}/connect/authorize", UriKind.Absolute),
                            TokenUrl         = new Uri($"{document.Servers[0].Url}/connect/token", UriKind.Absolute),
                            Scopes           = new Dictionary<string, string>
                            {
                                { "profile", "User profile information" },
                                { "email",   "User e-mail address" },
                                { "wallet",  "Wallet-specific claims (phone number)" },
                            },
                        },
                    },
                };

                document.AddComponent("OAuth2", oauthScheme);

                // Global security requirement — every operation requires this scheme
                // unless explicitly overridden with [AllowAnonymous] / no-auth at operation level.
                document.Security ??= [];
                document.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("OAuth2", document)] = ["profile", "email", "wallet"],
                });

                return Task.CompletedTask;
            });
        });

        return services;
    }

    /// <summary>
    /// Maps GET /openapi/v1.json and the interactive Scalar UI at GET /scalar/v1.
    /// </summary>
    internal static IEndpointRouteBuilder MapApiDocs(this IEndpointRouteBuilder app)
    {
        // GET /openapi/v1.json — raw OpenAPI document (JSON)
        app.MapOpenApi();

        // GET /scalar/v1 — interactive Scalar UI.
        // AddOAuth2Authentication wires the "Authorize" button to the authorization code + PKCE flow.
        // Scalar generates the code_challenge/code_verifier pair natively and handles the exchange.
        app.MapScalarApiReference(opts =>
        {
            opts.WithTitle("E-Wallet API")
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                .AddPreferredSecuritySchemes(["OAuth2"])
                // Scalar reads the authorizationCode flow + handles PKCE natively.
                // AddAuthorizationCodeFlow pre-fills the client_id in the Authorize dialog.
                .AddAuthorizationCodeFlow("OAuth2", flow =>
                    flow.WithClientId("ewallet-client"));
        });

        return app;
    }
}
