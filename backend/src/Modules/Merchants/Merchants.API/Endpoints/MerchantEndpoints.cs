using System.Security.Claims;
using EWallet.Modules.Merchants.Application.Commands.ApproveMerchant;
using EWallet.Modules.Merchants.Application.Commands.ApprovePaymentRequest;
using EWallet.Modules.Merchants.Application.Commands.CreatePaymentRequest;
using EWallet.Modules.Merchants.Application.Commands.RegisterMerchant;
using EWallet.Modules.Merchants.Application.Commands.RejectPaymentRequest;
using EWallet.Modules.Merchants.Application.Commands.SuspendMerchant;
using EWallet.Modules.Merchants.Application.Queries.GetMerchantById;
using EWallet.Modules.Merchants.Application.Queries.GetPaymentRequestById;
using EWallet.Modules.Merchants.Application.Queries.GetPendingPaymentRequestsForCustomer;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EWallet.Modules.Merchants.API.Endpoints;

public static class MerchantEndpoints
{
    public static IEndpointRouteBuilder MapMerchantEndpoints(this IEndpointRouteBuilder app)
    {
        var merchants = app.MapGroup("/api/v1/merchants")
            .WithTags("Merchants")
            .RequireAuthorization();

        var paymentRequests = app.MapGroup("/api/v1/payment-requests")
            .WithTags("PaymentRequests")
            .RequireAuthorization();

        // POST /api/v1/merchants — customer registers a merchant (self-service)
        merchants.MapPost("", async (
            RegisterMerchantRequest request,
            HttpContext context,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = context.User.GetUserId();
            var command = new RegisterMerchantCommand(userId, request.BusinessName, request.ReceivingWalletPhoneNumber, request.CallbackUrl);
            var result = await mediator.Send(command, ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/merchants/{result.Value.MerchantId}", result.Value)
                : MapError(result.Error);
        })
        .WithName("RegisterMerchant")
        .Produces<RegisterMerchantResponse>(StatusCodes.Status201Created);

        // GET /api/v1/merchants/{id} — admin only
        merchants.MapGet("{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetMerchantByIdQuery(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { result.Error.Code, result.Error.Description });
        })
        .WithName("GetMerchantById")
        .RequireAuthorization("Admin");

        // PATCH /api/v1/merchants/{id}/approve — admin only
        merchants.MapMethods("{id:guid}/approve", ["PATCH"], async (
            Guid id,
            HttpContext context,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var adminId = context.User.GetUserId();
            var result = await mediator.Send(new ApproveMerchantCommand(id, adminId), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : MapError(result.Error);
        })
        .WithName("ApproveMerchant")
        .RequireAuthorization("Admin");

        // PATCH /api/v1/merchants/{id}/suspend — admin only
        merchants.MapMethods("{id:guid}/suspend", ["PATCH"], async (
            Guid id,
            HttpContext context,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var adminId = context.User.GetUserId();
            var result = await mediator.Send(new SuspendMerchantCommand(id, adminId), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : MapError(result.Error);
        })
        .WithName("SuspendMerchant")
        .RequireAuthorization("Admin");

        // POST /api/v1/payment-requests — merchant client credentials
        paymentRequests.MapPost("", async (
            CreatePaymentRequestRequest request,
            HttpContext context,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var merchantId = context.User.GetMerchantId();
            if (merchantId == Guid.Empty)
                return Results.Unauthorized();

            var command = new CreatePaymentRequestCommand(merchantId, request.CustomerPhoneNumber, request.Amount);
            var result = await mediator.Send(command, ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/payment-requests/{result.Value.Id}", result.Value)
                : MapError(result.Error);
        })
        .WithName("CreatePaymentRequest")
        .RequireAuthorization("MerchantClient")
        .Produces<CreatePaymentRequestResponse>(StatusCodes.Status201Created);

        // GET /api/v1/payment-requests/{id}
        paymentRequests.MapGet("{id:guid}", async (
            Guid id,
            HttpContext context,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = context.User.GetUserId();
            var merchantId = context.User.GetMerchantId();
            var isMerchant = merchantId != Guid.Empty;

            var query = new GetPaymentRequestByIdQuery(id, userId, IsMerchant: isMerchant, MerchantId: isMerchant ? merchantId : null);
            var result = await mediator.Send(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { result.Error.Code, result.Error.Description });
        })
        .WithName("GetPaymentRequestById");

        // GET /api/v1/payment-requests/pending — customer sees their pending requests
        paymentRequests.MapGet("pending", async (
            HttpContext context,
            IMediator mediator,
            CancellationToken ct) =>
        {
            // Resolve customer phone from wallet — we need the phone number
            // The customer's phone_number claim is in the JWT (wallet scope)
            var phoneNumber = context.User.FindFirstValue("phone_number")
                              ?? context.User.FindFirstValue(ClaimTypes.MobilePhone)
                              ?? string.Empty;

            if (string.IsNullOrEmpty(phoneNumber))
                return Results.BadRequest(new { Code = "Merchant.PhoneRequired", Description = "Phone number claim not found in token." });

            var result = await mediator.Send(new GetPendingPaymentRequestsForCustomerQuery(phoneNumber), ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { result.Error.Code, result.Error.Description });
        })
        .WithName("GetPendingPaymentRequests");

        // POST /api/v1/payment-requests/{id}/approve — customer approves
        paymentRequests.MapPost("{id:guid}/approve", async (
            Guid id,
            HttpContext context,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = context.User.GetUserId();
            var result = await mediator.Send(new ApprovePaymentRequestCommand(id, userId), ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : MapError(result.Error);
        })
        .WithName("ApprovePaymentRequest");

        // POST /api/v1/payment-requests/{id}/reject — customer rejects
        paymentRequests.MapPost("{id:guid}/reject", async (
            Guid id,
            HttpContext context,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = context.User.GetUserId();
            var result = await mediator.Send(new RejectPaymentRequestCommand(id, userId), ct);

            return result.IsSuccess
                ? Results.NoContent()
                : MapError(result.Error);
        })
        .WithName("RejectPaymentRequest");

        return app;
    }

    private static IResult MapError(EWallet.BuildingBlocks.Common.Error error)
    {
        return error.Code.Contains("NotFound")
            ? Results.NotFound(new { error.Code, error.Description })
            : error.Code.Contains("Unauthorized")
                ? Results.Forbid()
                : error.Code.Contains("DuplicatePending")
                    ? Results.Conflict(new { error.Code, error.Description })
                    : Results.BadRequest(new { error.Code, error.Description });
    }
}

public sealed record RegisterMerchantRequest(string BusinessName, string ReceivingWalletPhoneNumber, string CallbackUrl);
public sealed record CreatePaymentRequestRequest(string CustomerPhoneNumber, decimal Amount);

internal static class MerchantClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    public static Guid GetMerchantId(this ClaimsPrincipal user)
    {
        var merchantId = user.FindFirstValue("merchant_id");
        return Guid.TryParse(merchantId, out var id) ? id : Guid.Empty;
    }
}
