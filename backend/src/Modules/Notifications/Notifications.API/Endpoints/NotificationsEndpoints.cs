using EWallet.Modules.Notifications.Application.Commands.MarkAllNotificationsAsRead;
using EWallet.Modules.Notifications.Application.Commands.MarkNotificationAsRead;
using EWallet.Modules.Notifications.Application.Queries.GetNotificationHistory;
using EWallet.Modules.Notifications.Application.Queries.GetUnreadCount;
using EWallet.Modules.Notifications.Infrastructure.Hubs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EWallet.Modules.Notifications.API.Endpoints;

public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHub<NotificationsHub>("/hubs/notifications");

        var group = app.MapGroup("/api/v1/notifications").WithTags("Notifications").RequireAuthorization();

        group.MapGet("", async (
            int? page,
            int? pageSize,
            HttpContext context,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = context.User.GetNotificationUserId();
            var query = new GetNotificationHistoryQuery(userId, page < 1 ? 1 : page ?? 1, pageSize < 1 ? 20 : pageSize ?? 20);
            var result = await mediator.Send(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { result.Error.Code, result.Error.Description });
        })
        .WithName("GetNotificationHistory");

        group.MapGet("/unread-count", async (
            HttpContext context,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = context.User.GetNotificationUserId();
            var query = new GetUnreadCountQuery(userId);
            var result = await mediator.Send(query, ct);

            return result.IsSuccess
                ? Results.Ok(new { Count = result.Value })
                : Results.BadRequest(new { result.Error.Code, result.Error.Description });
        })
        .WithName("GetUnreadNotificationCount");

        group.MapPut("/{id:guid}/read", async (
            Guid id,
            HttpContext context,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = context.User.GetNotificationUserId();
            var command = new MarkNotificationAsReadCommand(id, userId);
            var result = await mediator.Send(command, ct);

            if (result.IsSuccess) return Results.NoContent();
            if (result.Error.Code.Contains("NotFound")) return Results.NotFound(new { result.Error.Code, result.Error.Description });
            if (result.Error.Code.Contains("Forbidden")) return Results.Json(new { result.Error.Code, result.Error.Description }, statusCode: StatusCodes.Status403Forbidden);
            return Results.BadRequest(new { result.Error.Code, result.Error.Description });
        })
        .WithName("MarkNotificationAsRead");

        group.MapPut("/read-all", async (
            HttpContext context,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = context.User.GetNotificationUserId();
            var command = new MarkAllNotificationsAsReadCommand(userId);
            await mediator.Send(command, ct);
            return Results.NoContent();
        })
        .WithName("MarkAllNotificationsAsRead");

        return app;
    }
}

internal static class NotificationClaimsPrincipalExtensions
{
    public static Guid GetNotificationUserId(this System.Security.Claims.ClaimsPrincipal user)
    {
        var sub = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
