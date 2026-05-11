using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.Modules.Notifications.Application.Abstractions;

namespace EWallet.Modules.Notifications.Application.Commands.MarkAllNotificationsAsRead;

internal sealed class MarkAllNotificationsAsReadCommandHandler(INotificationRepository notificationRepository)
    : ICommandHandler<MarkAllNotificationsAsReadCommand>
{
    public async Task<Result> Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        await notificationRepository.MarkAllAsReadAsync(request.UserId, cancellationToken);
        return Result.Success();
    }
}
