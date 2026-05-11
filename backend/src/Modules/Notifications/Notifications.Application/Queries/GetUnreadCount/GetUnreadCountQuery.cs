using EWallet.BuildingBlocks.Application.Abstractions;

namespace EWallet.Modules.Notifications.Application.Queries.GetUnreadCount;

public sealed record GetUnreadCountQuery(Guid UserId) : IQuery<int>;
