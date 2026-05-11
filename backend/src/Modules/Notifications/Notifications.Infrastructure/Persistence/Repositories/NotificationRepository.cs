using EWallet.BuildingBlocks.Common;
using EWallet.Modules.Notifications.Application.Abstractions;
using EWallet.Modules.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EWallet.Modules.Notifications.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRepository(NotificationsDbContext context) : INotificationRepository
{
    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
        => await context.Notifications.AddAsync(notification, cancellationToken);

    public async Task<PagedResult<Notification>> GetByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Notification>(items, page, pageSize, totalCount);
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}
