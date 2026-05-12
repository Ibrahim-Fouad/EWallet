using EWallet.Modules.Merchants.Application.Jobs;
using EWallet.Modules.Merchants.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EWallet.Modules.Merchants.Infrastructure.Jobs;

[AutomaticRetry(Attempts = 0)]
public sealed class ExpirePaymentRequestJob(
    MerchantsDbContext dbContext,
    ILogger<ExpirePaymentRequestJob> logger)
    : IExpirePaymentRequestJob
{
    public async Task RunAsync(Guid requestId)
    {
        var request = await dbContext.PaymentRequests
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request is null)
        {
            logger.LogWarning("ExpirePaymentRequestJob: request {RequestId} not found", requestId);
            return;
        }

        // MarkExpired is idempotent — no-op if already resolved
        request.MarkExpired();

        await dbContext.DispatchDomainEventsAsync();
        await dbContext.SaveChangesAsync();

        logger.LogInformation("ExpirePaymentRequestJob completed for {RequestId} — status: {Status}",
            requestId, request.Status);
    }
}
