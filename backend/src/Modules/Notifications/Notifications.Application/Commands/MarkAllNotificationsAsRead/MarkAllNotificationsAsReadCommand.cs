using EWallet.BuildingBlocks.Application.Abstractions;

namespace EWallet.Modules.Notifications.Application.Commands.MarkAllNotificationsAsRead;

public sealed record MarkAllNotificationsAsReadCommand(Guid UserId) : ICommand;
