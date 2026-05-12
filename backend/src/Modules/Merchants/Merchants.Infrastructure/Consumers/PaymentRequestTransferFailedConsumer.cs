using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Merchants.Domain.Enums;
using EWallet.Modules.Merchants.Infrastructure.Persistence;
using EWallet.Modules.Notifications.Application.Abstractions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EWallet.Modules.Merchants.Infrastructure.Consumers;

public sealed class PaymentRequestTransferFailedConsumer(
    MerchantsDbContext dbContext,
    IWalletLookupService walletLookupService,
    INotificationService notificationService,
    ILogger<PaymentRequestTransferFailedConsumer> logger)
    : IConsumer<TransferFailedEvent>
{
    public async Task Consume(ConsumeContext<TransferFailedEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        var request = await dbContext.PaymentRequests
            .FirstOrDefaultAsync(r => r.TransferTransactionId == msg.TransactionId, ct);

        if (request is null)
            return;

        if (request.Status != PaymentRequestStatus.Approved)
        {
            logger.LogWarning(
                "PaymentRequest {RequestId} has status {Status} when TransferFailed arrived — skipping",
                request.Id, request.Status);
            return;
        }

        request.MarkFailed(msg.FailureReason);
        await dbContext.DispatchDomainEventsAsync(ct);
        await dbContext.SaveChangesAsync(ct);

        var customerWalletResult = await walletLookupService.GetByIdAsync(request.CustomerWalletId, ct);
        if (customerWalletResult.IsSuccess)
        {
            try
            {
                await notificationService.SendPaymentRequestResolvedAsync(
                    customerWalletResult.Value.OwnerId,
                    request.Id,
                    request.Status.ToString(),
                    ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to send PaymentRequestResolved SignalR notification for request {RequestId}",
                    request.Id);
            }
        }

        logger.LogInformation(
            "PaymentRequest {RequestId} marked Failed via transfer {TransactionId}: {Reason}",
            request.Id, msg.TransactionId, msg.FailureReason);
    }
}
