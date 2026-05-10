namespace EWallet.Modules.Notifications.Application.Abstractions;

public interface INotificationService
{
    Task SendTransferReceivedAsync(
        Guid recipientUserId,
        Guid transactionId,
        decimal amount,
        string currency,
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
}
