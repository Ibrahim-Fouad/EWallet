using EWallet.Modules.Merchants.Domain.Enums;
using EWallet.Modules.Merchants.Domain.Events;
using EWallet.Modules.Merchants.Domain.Repositories;
using EWallet.Modules.Notifications.Application.Abstractions;
using EWallet.Modules.Notifications.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EWallet.Modules.Merchants.Application.Events;

internal sealed class PaymentRequestResolvedNotificationHandler(
    IPaymentRequestRepository paymentRequestRepository,
    INotificationService notificationService,
    ILogger<PaymentRequestResolvedNotificationHandler> logger)
    : INotificationHandler<PaymentRequestResolvedEvent>
{
    public async Task Handle(PaymentRequestResolvedEvent notification, CancellationToken cancellationToken)
    {
        var actionStatus = notification.Status switch
        {
            PaymentRequestStatus.Approved  => NotificationActionStatus.Approved,
            PaymentRequestStatus.Rejected  => NotificationActionStatus.Rejected,
            PaymentRequestStatus.Expired   => NotificationActionStatus.Expired,
            PaymentRequestStatus.Completed => NotificationActionStatus.Completed,
            PaymentRequestStatus.Failed    => NotificationActionStatus.Failed,
            _ => (NotificationActionStatus?)null
        };

        if (actionStatus is null)
        {
            logger.LogWarning(
                "PaymentRequestResolvedNotificationHandler: unhandled status {Status} for {PaymentRequestId}",
                notification.Status, notification.PaymentRequestId);
            return;
        }

        // Retrieve TransferTransactionId for Completed/Failed transitions
        Guid? transactionId = null;
        if (actionStatus is NotificationActionStatus.Completed or NotificationActionStatus.Failed)
        {
            var request = await paymentRequestRepository.GetByIdAsync(
                notification.PaymentRequestId, cancellationToken);
            transactionId = request?.TransferTransactionId;
        }

        await notificationService.UpdatePaymentRequestStatusAsync(
            notification.PaymentRequestId,
            actionStatus.Value,
            transactionId,
            notification.ResolvedAt,
            cancellationToken);
    }
}
