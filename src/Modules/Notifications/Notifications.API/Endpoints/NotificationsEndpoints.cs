using EWallet.Modules.Notifications.Infrastructure.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace EWallet.Modules.Notifications.API.Endpoints;

public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHub<NotificationsHub>("/hubs/notifications");
        return app;
    }
}
