using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.Modules.Notifications.Application.Abstractions;

namespace EWallet.Modules.Notifications.Application.Commands.MarkNotificationAsRead;

internal sealed class MarkNotificationAsReadCommandHandler(INotificationRepository notificationRepository)
    : ICommandHandler<MarkNotificationAsReadCommand>
{
    public async Task<Result> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification is null)
            return Result.Failure(Error.NotFound("Notification.NotFound", "Notification not found."));

        if (notification.UserId != request.RequestingUserId)
            return Result.Failure(Error.Unauthorized("Notification.Forbidden", "You do not have access to this notification."));

        notification.MarkAsRead();
        await notificationRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
