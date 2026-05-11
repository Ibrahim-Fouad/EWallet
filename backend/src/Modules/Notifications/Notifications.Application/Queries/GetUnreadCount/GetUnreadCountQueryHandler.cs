using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.Modules.Notifications.Application.Abstractions;

namespace EWallet.Modules.Notifications.Application.Queries.GetUnreadCount;

internal sealed class GetUnreadCountQueryHandler(INotificationRepository notificationRepository)
    : IQueryHandler<GetUnreadCountQuery, int>
{
    public async Task<Result<int>> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var count = await notificationRepository.GetUnreadCountAsync(request.UserId, cancellationToken);
        return Result.Success(count);
    }
}
