using EWallet.BuildingBlocks.Common;
using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Notifications.Application.Abstractions;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EWallet.Modules.Notifications.Infrastructure.Consumers;

public sealed class TransferFailedConsumer(
    IWalletLookupService walletLookupService,
    INotificationService notificationService,
    ILogger<TransferFailedConsumer> logger)
    : IConsumer<TransferFailedEvent>
{
    public async Task Consume(ConsumeContext<TransferFailedEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        // Merchant-payment failures are surfaced via the payment-request notification — suppress default
        if (msg.Origin == TransferOrigin.MerchantPayment) return;

        var sourceWalletResult = await walletLookupService.GetByIdAsync(msg.SourceWalletId, ct);
        if (sourceWalletResult.IsFailure)
        {
            logger.LogWarning(
                "Could not find source wallet {WalletId} for failure notification on transaction {TransactionId}",
                msg.SourceWalletId, msg.TransactionId);
            return;
        }

        var senderUserId = sourceWalletResult.Value.OwnerId;

        await notificationService.SendTransactionFailedAsync(
            senderUserId,
            msg.TransactionId,
            msg.FailureReason,
            ct);

        logger.LogInformation(
            "Transfer failure notification sent to sender {SenderId} for transaction {TransactionId}",
            senderUserId, msg.TransactionId);
    }
}
