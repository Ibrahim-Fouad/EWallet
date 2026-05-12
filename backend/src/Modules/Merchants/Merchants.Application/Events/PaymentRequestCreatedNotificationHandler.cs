using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Merchants.Domain.Events;
using EWallet.Modules.Merchants.Domain.Repositories;
using EWallet.Modules.Notifications.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EWallet.Modules.Merchants.Application.Events;

internal sealed class PaymentRequestCreatedNotificationHandler(
    IMerchantRepository merchantRepository,
    IWalletLookupService walletLookupService,
    INotificationService notificationService,
    ILogger<PaymentRequestCreatedNotificationHandler> logger)
    : INotificationHandler<PaymentRequestCreatedEvent>
{
    public async Task Handle(PaymentRequestCreatedEvent notification, CancellationToken cancellationToken)
    {
        var merchant = await merchantRepository.GetByIdAsync(notification.MerchantId, cancellationToken);
        if (merchant is null)
        {
            logger.LogWarning(
                "PaymentRequestCreatedNotificationHandler: merchant {MerchantId} not found for request {PaymentRequestId}",
                notification.MerchantId, notification.PaymentRequestId);
            return;
        }

        var walletResult = await walletLookupService.GetByPhoneNumberAsync(
            notification.CustomerPhoneNumber, cancellationToken);
        if (walletResult.IsFailure)
        {
            logger.LogWarning(
                "PaymentRequestCreatedNotificationHandler: wallet not found for phone {Phone}, request {PaymentRequestId}",
                notification.CustomerPhoneNumber, notification.PaymentRequestId);
            return;
        }

        await notificationService.SendPaymentRequestCreatedAsync(
            walletResult.Value.OwnerId,
            notification.PaymentRequestId,
            merchant.BusinessName,
            notification.Amount,
            notification.Currency,
            notification.ExpiresAt,
            cancellationToken);
    }
}
