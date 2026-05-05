using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Notifications.Application.Abstractions;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EWallet.Modules.Notifications.Infrastructure.Consumers;

public sealed class TransferCompletedConsumer(
    IWalletLookupService walletLookupService,
    INotificationService notificationService,
    ILogger<TransferCompletedConsumer> logger)
    : IConsumer<TransferCompletedEvent>
{
    public async Task Consume(ConsumeContext<TransferCompletedEvent> context)
    {
        var msg = context.Message;

        var recipientWalletResult = await walletLookupService.GetByIdAsync(
            msg.DestinationWalletId,
            context.CancellationToken);

        if (recipientWalletResult.IsFailure)
        {
            logger.LogWarning(
                "Could not find destination wallet {WalletId} for notification on transaction {TransactionId}",
                msg.DestinationWalletId,
                msg.TransactionId);
            return;
        }

        var recipientUserId = recipientWalletResult.Value.OwnerId;

        await notificationService.SendTransferReceivedAsync(
            recipientUserId,
            msg.TransactionId,
            msg.Amount,
            msg.Currency,
            context.CancellationToken);

        logger.LogInformation(
            "Transfer notification sent to user {UserId} for transaction {TransactionId}",
            recipientUserId,
            msg.TransactionId);
    }
}
