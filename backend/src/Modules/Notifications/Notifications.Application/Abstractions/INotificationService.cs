using EWallet.BuildingBlocks.Common;
using EWallet.Modules.Notifications.Domain.Enums;

namespace EWallet.Modules.Notifications.Application.Abstractions;

public interface INotificationService
{
    Task SendTransferReceivedAsync(
        Guid recipientUserId,
        Guid transactionId,
        decimal amount,
        string currency,
        Guid sourceWalletId,
        string senderPhoneNumber,
        CancellationToken cancellationToken = default);

    Task SendTransactionCompletedAsync(
        Guid senderUserId,
        Guid transactionId,
        decimal amount,
        string currency,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default);

    Task SendTransactionFailedAsync(
        Guid senderUserId,
        Guid transactionId,
        string failureReason,
        CancellationToken cancellationToken = default);

    Task<Result<Guid>> SendPaymentRequestCreatedAsync(
        Guid customerUserId,
        Guid paymentRequestId,
        string merchantName,
        decimal amount,
        string currency,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    Task<Result> UpdatePaymentRequestStatusAsync(
        Guid paymentRequestId,
        NotificationActionStatus newStatus,
        Guid? transactionId,
        DateTimeOffset takenAt,
        CancellationToken cancellationToken = default);
}
