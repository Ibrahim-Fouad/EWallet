using EWallet.Modules.Wallets.Application.Commands.CreateWallet;
using EWallet.Modules.Wallets.Application.Commands.Deposit;
using EWallet.Modules.Wallets.Application.Queries.GetWalletById;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EWallet.Modules.Wallets.API.Endpoints;

public static class WalletEndpoints
{
    public static IEndpointRouteBuilder MapWalletEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/wallets").WithTags("Wallets").RequireAuthorization();

        group.MapPost("/", async (CreateWalletRequest request, IMediator mediator, HttpContext context, CancellationToken ct) =>
        {
            var userId = context.User.GetUserId();
            var command = new CreateWalletCommand(userId, request.PhoneNumber, request.Currency);
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/wallets/{result.Value.WalletId}", result.Value)
                : Results.BadRequest(new { result.Error.Code, result.Error.Description });
        })
        .WithName("CreateWallet")
        .Produces<CreateWalletResponse>(StatusCodes.Status201Created);

        group.MapGet("/{walletId:guid}", async (Guid walletId, IMediator mediator, HttpContext context, CancellationToken ct) =>
        {
            var userId = context.User.GetUserId();
            var query = new GetWalletByIdQuery(walletId, userId);
            var result = await mediator.Send(query, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { result.Error.Code, result.Error.Description });
        })
        .WithName("GetWalletById");

        group.MapPost("/{walletId:guid}/deposit", async (Guid walletId, DepositRequest request, IMediator mediator, HttpContext context, CancellationToken ct) =>
        {
            var userId = context.User.GetUserId();
            var command = new DepositFundsCommand(walletId, userId, request.Amount);
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { result.Error.Code, result.Error.Description });
        })
        .WithName("DepositFunds");

        return app;
    }
}

public sealed record CreateWalletRequest(string PhoneNumber, EWallet.Modules.Wallets.Domain.Enums.Currency Currency);
public sealed record DepositRequest(decimal Amount);

internal static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this System.Security.Claims.ClaimsPrincipal user)
    {
        var sub = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
