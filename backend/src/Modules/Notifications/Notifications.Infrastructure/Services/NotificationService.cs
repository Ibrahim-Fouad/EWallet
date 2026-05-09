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
        CancellationToken cancellationToken = default)
    {
        await hubContext.Clients
            .Group(recipientUserId.ToString())
            .SendAsync("TransferReceived", new
            {
                TransactionId = transactionId,
                Amount = amount,
                Currency = currency,
                ReceivedAt = DateTimeOffset.UtcNow
            }, cancellationToken);
    }
}
