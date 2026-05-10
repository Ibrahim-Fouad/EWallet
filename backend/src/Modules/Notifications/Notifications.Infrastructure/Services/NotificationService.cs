using EWallet.Modules.Notifications.Application.Abstractions;
using EWallet.Modules.Notifications.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EWallet.Modules.Notifications.Infrastructure.Services;

internal sealed class NotificationService(IHubContext<NotificationsHub> hubContext) : INotificationService
{
    public async Task SendTransferReceivedAsync(
        Guid recipientUserId,
        Guid transactionId,
        decimal amount,
        string currency,
        string senderPhoneNumber,
        CancellationToken cancellationToken = default)
    {
        await hubContext.Clients
            .Group(recipientUserId.ToString())
            .SendAsync("TransferReceived", new
            {
                TransactionId = transactionId,
                Amount = amount,
                Currency = currency,
                SenderPhoneNumber = senderPhoneNumber,
                ReceivedAt = DateTimeOffset.UtcNow
            }, cancellationToken);
    }

    public async Task SendTransactionCompletedAsync(
        Guid senderUserId,
        Guid transactionId,
        decimal amount,
        string currency,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default)
    {
        await hubContext.Clients
            .Group(senderUserId.ToString())
            .SendAsync("TransactionCompleted", new
            {
                TransactionId = transactionId,
                Amount = amount,
                Currency = currency,
                CompletedAt = completedAt
            }, cancellationToken);
    }

    public async Task SendTransactionFailedAsync(
        Guid senderUserId,
        Guid transactionId,
        string failureReason,
        CancellationToken cancellationToken = default)
    {
        await hubContext.Clients
            .Group(senderUserId.ToString())
            .SendAsync("TransactionFailed", new
            {
                TransactionId = transactionId,
                FailureReason = failureReason
            }, cancellationToken);
    }
}
