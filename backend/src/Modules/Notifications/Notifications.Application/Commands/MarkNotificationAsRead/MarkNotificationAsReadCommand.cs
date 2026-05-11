using EWallet.BuildingBlocks.Application.Abstractions;

namespace EWallet.Modules.Notifications.Application.Commands.MarkNotificationAsRead;

public sealed record MarkNotificationAsReadCommand(
    Guid NotificationId,
    Guid RequestingUserId) : ICommand;
