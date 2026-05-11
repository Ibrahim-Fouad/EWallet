using EWallet.BuildingBlocks.Domain.Abstractions;
using EWallet.Modules.Notifications.Domain.Enums;

namespace EWallet.Modules.Notifications.Domain.Entities;

public sealed class Notification : Entity
{
    private Notification() { }

    private Notification(
        Guid id,
        Guid userId,
        NotificationType type,
        Guid transactionId,
        decimal? amount,
        string? currency,
        Guid? sourceWalletId,
        DateTimeOffset? completedAt,
        DateTimeOffset? receivedAt,
        string? failureReason) : base(id)
    {
        UserId = userId;
        Type = type;
        TransactionId = transactionId;
        Amount = amount;
        Currency = currency;
        SourceWalletId = sourceWalletId;
        CompletedAt = completedAt;
        ReceivedAt = receivedAt;
        FailureReason = failureReason;
        IsRead = false;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public Guid TransactionId { get; private set; }
    public decimal? Amount { get; private set; }
    public string? Currency { get; private set; }

    // Stored instead of phone number — resolved fresh at read time via IWalletLookupService
    public Guid? SourceWalletId { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? ReceivedAt { get; private set; }
    public string? FailureReason { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static Notification TransferReceived(
        Guid recipientUserId,
        Guid transactionId,
        decimal amount,
        string currency,
        Guid sourceWalletId) =>
        new(
            Guid.CreateVersion7(),
            recipientUserId,
            NotificationType.TransferReceived,
            transactionId,
            amount,
            currency,
            sourceWalletId,
            completedAt: null,
            receivedAt: DateTimeOffset.UtcNow,
            failureReason: null);

    public static Notification TransactionCompleted(
        Guid senderUserId,
        Guid transactionId,
        decimal amount,
        string currency,
        DateTimeOffset completedAt) =>
        new(
            Guid.CreateVersion7(),
            senderUserId,
            NotificationType.TransactionCompleted,
            transactionId,
            amount,
            currency,
            sourceWalletId: null,
            completedAt,
            receivedAt: null,
            failureReason: null);

    public static Notification TransactionFailed(
        Guid senderUserId,
        Guid transactionId,
        string failureReason) =>
        new(
            Guid.CreateVersion7(),
            senderUserId,
            NotificationType.TransactionFailed,
            transactionId,
            amount: null,
            currency: null,
            sourceWalletId: null,
            completedAt: null,
            receivedAt: null,
            failureReason);

    public void MarkAsRead() => IsRead = true;
}
