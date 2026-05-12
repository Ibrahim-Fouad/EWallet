using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Merchants.Domain.Enums;
using EWallet.Modules.Merchants.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EWallet.Modules.Merchants.Infrastructure.Consumers;

public sealed class PaymentRequestTransferFailedConsumer(
    MerchantsDbContext dbContext,
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

        // PaymentRequestResolvedEvent(Failed) is dispatched here;
        // PaymentRequestResolvedNotificationHandler updates the notification row.
        await dbContext.DispatchDomainEventsAsync(ct);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "PaymentRequest {RequestId} marked Failed via transfer {TransactionId}: {Reason}",
            request.Id, msg.TransactionId, msg.FailureReason);
    }
}
