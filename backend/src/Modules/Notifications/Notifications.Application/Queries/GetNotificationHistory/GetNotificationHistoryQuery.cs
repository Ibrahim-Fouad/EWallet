using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;

namespace EWallet.Modules.Notifications.Application.Queries.GetNotificationHistory;

public sealed record GetNotificationHistoryQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<NotificationDto>>;
