using EWallet.BuildingBlocks.Common;
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
        var ct = context.CancellationToken;

        // Merchant-payment transfers get their own interactive notification — suppress default ones
        if (msg.Origin == TransferOrigin.MerchantPayment) return;

        var sourceWalletResult = await walletLookupService.GetByIdAsync(msg.SourceWalletId, ct);
        if (sourceWalletResult.IsFailure)
        {
            logger.LogWarning(
                "Could not find source wallet {WalletId} for completion notification on transaction {TransactionId}",
                msg.SourceWalletId, msg.TransactionId);
            return;
        }

        var destinationWalletResult = await walletLookupService.GetByIdAsync(msg.DestinationWalletId, ct);
        if (destinationWalletResult.IsFailure)
        {
            logger.LogWarning(
                "Could not find destination wallet {WalletId} for completion notification on transaction {TransactionId}",
                msg.DestinationWalletId, msg.TransactionId);
            return;
        }

        var sourceWallet = sourceWalletResult.Value;
        var destinationWallet = destinationWalletResult.Value;

        await notificationService.SendTransactionCompletedAsync(
            sourceWallet.OwnerId,
            msg.TransactionId,
            msg.Amount,
            msg.Currency,
            msg.CompletedAt,
            ct);

        await notificationService.SendTransferReceivedAsync(
            destinationWallet.OwnerId,
            msg.TransactionId,
            msg.Amount,
            msg.Currency,
            sourceWallet.Id,
            sourceWallet.PhoneNumber,
            ct);

        logger.LogInformation(
            "Transfer notifications sent — completed to sender {SenderId}, received to recipient {RecipientId} for transaction {TransactionId}",
            sourceWallet.OwnerId, destinationWallet.OwnerId, msg.TransactionId);
    }
}
