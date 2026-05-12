using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Merchants.Domain.Enums;
using EWallet.Modules.Merchants.Infrastructure.Persistence;
using EWallet.Modules.Notifications.Application.Abstractions;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EWallet.Modules.Merchants.Infrastructure.Jobs;

[AutomaticRetry(Attempts = 0)]
public sealed class BackfillMissingPaymentRequestNotificationsJob(
    MerchantsDbContext dbContext,
    INotificationRepository notificationRepository,
    IWalletLookupService walletLookupService,
    INotificationService notificationService,
    ILogger<BackfillMissingPaymentRequestNotificationsJob> logger)
{
    public async Task RunAsync()
    {
        var lookback = DateTimeOffset.UtcNow.AddMinutes(-5);

        var recentPending = await dbContext.PaymentRequests
            .Where(r => r.Status == PaymentRequestStatus.Pending && r.CreatedAt >= lookback)
            .ToListAsync();

        if (recentPending.Count == 0) return;

        foreach (var request in recentPending)
        {
            var existing = await notificationRepository.GetByPaymentRequestIdAsync(request.Id);
            if (existing is not null) continue;

            logger.LogWarning(
                "BackfillMissingPaymentRequestNotificationsJob: backfilling notification for {PaymentRequestId}",
                request.Id);

            var walletResult = await walletLookupService.GetByIdAsync(request.CustomerWalletId);
            if (walletResult.IsFailure)
            {
                logger.LogWarning(
                    "BackfillMissingPaymentRequestNotificationsJob: wallet {WalletId} not found, skipping {PaymentRequestId}",
                    request.CustomerWalletId, request.Id);
                continue;
            }

            var merchantName = (await dbContext.Merchants
                .Where(m => m.Id == request.MerchantId)
                .Select(m => m.BusinessName)
                .FirstOrDefaultAsync()) ?? "Merchant";

            await notificationService.SendPaymentRequestCreatedAsync(
                walletResult.Value.OwnerId,
                request.Id,
                merchantName,
                request.Amount,
                request.Currency,
                request.ExpiresAt);
        }
    }
}
