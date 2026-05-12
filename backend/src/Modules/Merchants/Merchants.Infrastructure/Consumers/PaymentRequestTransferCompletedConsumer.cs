using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Merchants.Domain.Enums;
using EWallet.Modules.Merchants.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EWallet.Modules.Merchants.Infrastructure.Consumers;

public sealed class PaymentRequestTransferCompletedConsumer(
    MerchantsDbContext dbContext,
    ILogger<PaymentRequestTransferCompletedConsumer> logger)
    : IConsumer<TransferCompletedEvent>
{
    public async Task Consume(ConsumeContext<TransferCompletedEvent> context)
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
                "PaymentRequest {RequestId} has status {Status} when TransferCompleted arrived — skipping",
                request.Id, request.Status);
            return;
        }

        request.MarkCompleted();
        await dbContext.DispatchDomainEventsAsync(ct);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "PaymentRequest {RequestId} marked Completed via transfer {TransactionId}",
            request.Id, msg.TransactionId);
    }
}
