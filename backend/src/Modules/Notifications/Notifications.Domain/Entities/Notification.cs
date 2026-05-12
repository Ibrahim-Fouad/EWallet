using EWallet.BuildingBlocks.Domain.Abstractions;
using EWallet.Modules.Notifications.Domain.Enums;

namespace EWallet.Modules.Notifications.Domain.Entities;

public enum NotificationUpdateOutcome { Applied, NoChange, InvalidTransition }

public sealed class Notification : Entity
{
    private static readonly IReadOnlySet<NotificationActionStatus> TerminalStatuses =
        new HashSet<NotificationActionStatus>
        {
            NotificationActionStatus.Rejected,
            NotificationActionStatus.Expired,
            NotificationActionStatus.Completed,
            NotificationActionStatus.Failed
        };

    private Notification() { }

    private Notification(
        Guid id,
        Guid userId,
        NotificationType type,
        Guid? transactionId,
        decimal? amount,
        string? currency,
        Guid? sourceWalletId,
        DateTimeOffset? completedAt,
        DateTimeOffset? receivedAt,
        string? failureReason,
        Guid? paymentRequestId,
        string? merchantName,
        NotificationActionStatus? actionStatus,
        DateTimeOffset? expiresAt) : base(id)
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
        PaymentRequestId = paymentRequestId;
        MerchantName = merchantName;
        ActionStatus = actionStatus;
        ExpiresAt = expiresAt;
        IsRead = false;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public Guid? TransactionId { get; private set; }
    public decimal? Amount { get; private set; }
    public string? Currency { get; private set; }

    // Stored instead of phone number — resolved fresh at read time via IWalletLookupService
    public Guid? SourceWalletId { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? ReceivedAt { get; private set; }
    public string? FailureReason { get; private set; }

    public Guid? PaymentRequestId { get; private set; }
    public string? MerchantName { get; private set; }
    public NotificationActionStatus? ActionStatus { get; private set; }
    public DateTimeOffset? ActionTakenAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

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
            failureReason: null,
            paymentRequestId: null,
            merchantName: null,
            actionStatus: null,
            expiresAt: null);

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
            failureReason: null,
            paymentRequestId: null,
            merchantName: null,
            actionStatus: null,
            expiresAt: null);

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
            failureReason,
            paymentRequestId: null,
            merchantName: null,
            actionStatus: null,
            expiresAt: null);

    public static Notification PaymentRequestCreated(
        Guid customerUserId,
        Guid paymentRequestId,
        string merchantName,
        decimal amount,
        string currency,
        DateTimeOffset expiresAt) =>
        new(
            Guid.CreateVersion7(),
            customerUserId,
            NotificationType.PaymentRequestCreated,
            transactionId: null,
            amount,
            currency,
            sourceWalletId: null,
            completedAt: null,
            receivedAt: null,
            failureReason: null,
            paymentRequestId,
            merchantName,
            actionStatus: NotificationActionStatus.Pending,
            expiresAt);

    public NotificationUpdateOutcome TryUpdatePaymentRequestStatus(
        NotificationActionStatus newStatus,
        DateTimeOffset takenAt,
        Guid? transactionId = null)
    {
        var current = ActionStatus;
        if (current == newStatus) return NotificationUpdateOutcome.NoChange;

        bool allowed = current switch
        {
            NotificationActionStatus.Pending =>
                newStatus is NotificationActionStatus.Approved
                    or NotificationActionStatus.Rejected
                    or NotificationActionStatus.Expired,

            NotificationActionStatus.Approved =>
                newStatus is NotificationActionStatus.Completed
                    or NotificationActionStatus.Failed,

            _ when TerminalStatuses.Contains(current!.Value) => false,
            _ => false
        };

        if (!allowed) return NotificationUpdateOutcome.InvalidTransition;

        ActionStatus = newStatus;
        ActionTakenAt = takenAt;
        if (transactionId.HasValue)
            TransactionId = transactionId;
        IsRead = true;
        return NotificationUpdateOutcome.Applied;
    }

    public void MarkAsRead()
    {
        if (!IsRead) IsRead = true;
    }
}
