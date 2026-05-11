using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Notifications.Application.Abstractions;
using EWallet.Modules.Notifications.Domain.Enums;

namespace EWallet.Modules.Notifications.Application.Queries.GetNotificationHistory;

internal sealed class GetNotificationHistoryQueryHandler(
    INotificationRepository notificationRepository,
    IWalletLookupService walletLookupService)
    : IQueryHandler<GetNotificationHistoryQuery, PagedResult<NotificationDto>>
{
    public async Task<Result<PagedResult<NotificationDto>>> Handle(
        GetNotificationHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var paged = await notificationRepository.GetByUserIdAsync(
            request.UserId, request.Page, request.PageSize, cancellationToken);

        // Batch-resolve phone numbers for TransferReceived rows only — avoids N+1
        var walletIds = paged.Items
            .Where(n => n.Type == NotificationType.TransferReceived && n.SourceWalletId.HasValue)
            .Select(n => n.SourceWalletId!.Value)
            .Distinct()
            .ToList();

        IReadOnlyDictionary<Guid, WalletInfo> walletMap =
            walletIds.Count > 0
                ? await walletLookupService.GetByIdsAsync(walletIds, cancellationToken)
                : new Dictionary<Guid, WalletInfo>();

        var dtos = paged.Items.Select(n =>
        {
            string? senderPhone = null;
            if (n is { Type: NotificationType.TransferReceived, SourceWalletId: not null }
                && walletMap.TryGetValue(n.SourceWalletId.Value, out var wallet))
                senderPhone = wallet.PhoneNumber;

            return new NotificationDto(
                n.Id,
                n.Type,
                n.TransactionId,
                n.Amount,
                n.Currency,
                senderPhone,
                n.FailureReason,
                n.CompletedAt,
                n.ReceivedAt,
                n.IsRead,
                n.CreatedAt);
        }).ToList();

        return Result.Success(new PagedResult<NotificationDto>(dtos, paged.Page, paged.PageSize, paged.TotalCount));
    }
}
