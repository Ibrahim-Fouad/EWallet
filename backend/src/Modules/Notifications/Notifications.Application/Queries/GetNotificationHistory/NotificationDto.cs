using EWallet.Modules.Notifications.Domain.Enums;

namespace EWallet.Modules.Notifications.Application.Queries.GetNotificationHistory;

public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    Guid TransactionId,
    decimal? Amount,
    string? Currency,
    string? SenderPhoneNumber,
    string? FailureReason,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? ReceivedAt,
    bool IsRead,
    DateTimeOffset CreatedAt);
