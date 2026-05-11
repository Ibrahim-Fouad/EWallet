using EWallet.Modules.Notifications.Application.Abstractions;
using EWallet.Modules.Notifications.Domain.Entities;
using EWallet.Modules.Notifications.Infrastructure.Hubs;
using EWallet.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;

namespace EWallet.Modules.Notifications.Infrastructure.Services;

internal sealed class NotificationService(
    IHubContext<NotificationsHub> hubContext,
    INotificationRepository notificationRepository) : INotificationService
{
    public async Task SendTransferReceivedAsync(
        Guid recipientUserId,
        Guid transactionId,
        decimal amount,
        string currency,
        Guid sourceWalletId,
        string senderPhoneNumber,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.TransferReceived(
            recipientUserId, transactionId, amount, currency, sourceWalletId);

        await notificationRepository.AddAsync(notification, cancellationToken);
        await notificationRepository.SaveChangesAsync(cancellationToken);

        await hubContext.Clients
            .Group(recipientUserId.ToString())
            .SendAsync("TransferReceived", new
            {
                NotificationId = notification.Id,
                TransactionId = transactionId,
                Amount = amount,
                Currency = currency,
                SenderPhoneNumber = senderPhoneNumber,
                ReceivedAt = notification.ReceivedAt
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
        var notification = Notification.TransactionCompleted(
            senderUserId, transactionId, amount, currency, completedAt);

        await notificationRepository.AddAsync(notification, cancellationToken);
        await notificationRepository.SaveChangesAsync(cancellationToken);

        await hubContext.Clients
            .Group(senderUserId.ToString())
            .SendAsync("TransactionCompleted", new
            {
                NotificationId = notification.Id,
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
        var notification = Notification.TransactionFailed(senderUserId, transactionId, failureReason);

        await notificationRepository.AddAsync(notification, cancellationToken);
        await notificationRepository.SaveChangesAsync(cancellationToken);

        await hubContext.Clients
            .Group(senderUserId.ToString())
            .SendAsync("TransactionFailed", new
            {
                NotificationId = notification.Id,
                TransactionId = transactionId,
                FailureReason = failureReason
            }, cancellationToken);
    }
}
