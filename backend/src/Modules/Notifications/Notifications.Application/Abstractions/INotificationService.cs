namespace EWallet.Modules.Notifications.Application.Abstractions;

public interface INotificationService
{
    Task SendTransferReceivedAsync(
        Guid recipientUserId,
        Guid transactionId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default);
}
