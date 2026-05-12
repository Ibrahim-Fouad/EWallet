using EWallet.BuildingBlocks.Common;
using EWallet.Modules.Notifications.Application.Abstractions;
using EWallet.Modules.Notifications.Domain.Entities;
using EWallet.Modules.Notifications.Domain.Enums;
using EWallet.Modules.Notifications.Infrastructure.Hubs;
using EWallet.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EWallet.Modules.Notifications.Infrastructure.Services;

internal sealed class NotificationService(
    IHubContext<NotificationsHub> hubContext,
    INotificationRepository notificationRepository,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task SendTransferReceivedAsync(
        Guid recipientUserId,
        Guid transactionId,
        decimal amount,
        string currency,
        Guid sourceWalletId,
        string senderPhoneNumber,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.TransferReceived(
            recipientUserId, transactionId, amount, currency, sourceWalletId);

        await notificationRepository.AddAsync(notification, cancellationToken);
        await notificationRepository.SaveChangesAsync(cancellationToken);

        await hubContext.Clients
            .Group(recipientUserId.ToString())
            .SendAsync("TransferReceived", new
            {
                NotificationId = notification.Id,
                TransactionId = transactionId,
                Amount = amount,
                Currency = currency,
                SenderPhoneNumber = senderPhoneNumber,
                ReceivedAt = notification.ReceivedAt
            }, cancellationToken);
    }

    public async Task SendTransactionCompletedAsync(
        Guid senderUserId,
        Guid transactionId,
        decimal amount,
        string currency,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.TransactionCompleted(
            senderUserId, transactionId, amount, currency, completedAt);

        await notificationRepository.AddAsync(notification, cancellationToken);
        await notificationRepository.SaveChangesAsync(cancellationToken);

        await hubContext.Clients
            .Group(senderUserId.ToString())
            .SendAsync("TransactionCompleted", new
            {
                NotificationId = notification.Id,
                TransactionId = transactionId,
                Amount = amount,
                Currency = currency,
                CompletedAt = completedAt
            }, cancellationToken);
    }

    public async Task SendTransactionFailedAsync(
        Guid senderUserId,
        Guid transactionId,
        string failureReason,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.TransactionFailed(senderUserId, transactionId, failureReason);

        await notificationRepository.AddAsync(notification, cancellationToken);
        await notificationRepository.SaveChangesAsync(cancellationToken);

        await hubContext.Clients
            .Group(senderUserId.ToString())
            .SendAsync("TransactionFailed", new
            {
                NotificationId = notification.Id,
                TransactionId = transactionId,
                FailureReason = failureReason
            }, cancellationToken);
    }

    public async Task<Result<Guid>> SendPaymentRequestCreatedAsync(
        Guid customerUserId,
        Guid paymentRequestId,
        string merchantName,
        decimal amount,
        string currency,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        // Idempotent — return existing row if already created (safe for retries)
        var existing = await notificationRepository.GetByPaymentRequestIdAsync(paymentRequestId, cancellationToken);
        if (existing is not null)
            return Result.Success(existing.Id);

        var notification = Notification.PaymentRequestCreated(
            customerUserId, paymentRequestId, merchantName, amount, currency, expiresAt);

        await notificationRepository.AddAsync(notification, cancellationToken);
        await notificationRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Notifications.PaymentRequest.Created {PaymentRequestId} {CustomerUserId}",
            paymentRequestId, customerUserId);

        try
        {
            await hubContext.Clients
                .Group(customerUserId.ToString())
                .SendAsync("PaymentRequestCreated", new
                {
                    NotificationId = notification.Id,
                    PaymentRequestId = paymentRequestId,
                    MerchantName = merchantName,
                    Amount = amount,
                    Currency = currency,
                    ExpiresAt = expiresAt,
                    ActionStatus = "Pending",
                    CreatedAt = notification.CreatedAt
                }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Notifications.PaymentRequest.SignalRSendFailed {NotificationId}",
                notification.Id);
        }

        return Result.Success(notification.Id);
    }

    public async Task<Result> UpdatePaymentRequestStatusAsync(
        Guid paymentRequestId,
        NotificationActionStatus newStatus,
        Guid? transactionId,
        DateTimeOffset takenAt,
        CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            var notification = await notificationRepository.GetByPaymentRequestIdAsync(
                paymentRequestId, cancellationToken);

            if (notification is null)
            {
                logger.LogWarning(
                    "Notifications.PaymentRequest.RowMissing {PaymentRequestId}", paymentRequestId);
                return Result.Success();
            }

            var fromStatus = notification.ActionStatus;
            var outcome = notification.TryUpdatePaymentRequestStatus(newStatus, takenAt, transactionId);

            if (outcome == NotificationUpdateOutcome.NoChange)
                return Result.Success();

            if (outcome == NotificationUpdateOutcome.InvalidTransition)
            {
                logger.LogWarning(
                    "Notifications.PaymentRequest.UpdateInvalidTransition {PaymentRequestId} from {FromStatus} attempted {NewStatus}",
                    paymentRequestId, fromStatus, newStatus);
                return Result.Success();
            }

            try
            {
                await notificationRepository.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Notifications.PaymentRequest.UpdateApplied {PaymentRequestId} {FromStatus} -> {ToStatus}",
                    paymentRequestId, fromStatus, newStatus);

                try
                {
                    await hubContext.Clients
                        .Group(notification.UserId.ToString())
                        .SendAsync("PaymentRequestUpdated", new
                        {
                            NotificationId = notification.Id,
                            PaymentRequestId = paymentRequestId,
                            ActionStatus = newStatus.ToString(),
                            ActionTakenAt = takenAt,
                            TransactionId = transactionId
                        }, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Notifications.PaymentRequest.SignalRSendFailed {NotificationId}",
                        notification.Id);
                }

                return Result.Success();
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxRetries - 1)
            {
                // Reload and retry
            }
        }

        return Result.Success();
    }
}
