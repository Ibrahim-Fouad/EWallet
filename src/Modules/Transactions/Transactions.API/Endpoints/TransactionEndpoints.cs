using EWallet.Modules.Transactions.Application.Commands.Transfer;
using EWallet.Modules.Transactions.Application.Queries.GetTransactionById;
using EWallet.Modules.Transactions.Application.Queries.GetTransactionHistory;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EWallet.Modules.Transactions.API.Endpoints;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1").WithTags("Transactions").RequireAuthorization();

        group.MapPost("/transactions/transfer", async (
            TransferRequest request,
            HttpContext context,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var idempotencyKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                return Results.BadRequest(new { Code = "Transfer.MissingIdempotencyKey", Description = "Idempotency-Key header is required." });

            var userId = context.User.GetTransactionUserId();
            var command = new TransferCommand(idempotencyKey, request.SourceWalletId, request.DestinationWalletId, request.Amount, userId, request.Notes);
            var result = await mediator.Send(command, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { result.Error.Code, result.Error.Description });
        })
        .WithName("Transfer")
        .RequireRateLimiting("transfer")
        .Produces<TransferResponse>(StatusCodes.Status200OK);

        // GET /api/v1/transactions/{id} — poll for final status (Pending→Completed|Failed)
        group.MapGet("/transactions/{id:guid}", async (
            Guid id,
            HttpContext context,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = context.User.GetTransactionUserId();
            var query = new GetTransactionByIdQuery(id, userId);
            var result = await mediator.Send(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { result.Error.Code, result.Error.Description });
        })
        .WithName("GetTransactionById");

        group.MapGet("/wallets/{walletId:guid}/transactions", async (
            Guid walletId,
            int page,
            int pageSize,
            HttpContext context,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = context.User.GetTransactionUserId();
            var query = new GetTransactionHistoryQuery(walletId, userId, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize);
            var result = await mediator.Send(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { result.Error.Code, result.Error.Description });
        })
        .WithName("GetTransactionHistory");

        return app;
    }
}

public sealed record TransferRequest(Guid SourceWalletId, Guid DestinationWalletId, decimal Amount, string? Notes = null);

internal static class TransactionClaimsPrincipalExtensions
{
    public static Guid GetTransactionUserId(this System.Security.Claims.ClaimsPrincipal user)
    {
        var sub = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
