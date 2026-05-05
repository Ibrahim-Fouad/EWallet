using EWallet.BuildingBlocks.Common.Constants;
using EWallet.Modules.Wallets.Domain.Entities;
using EWallet.Modules.Wallets.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EWallet.Modules.Wallets.Infrastructure.Persistence.Repositories;

internal sealed class WalletRepository(WalletsDbContext context) : IWalletRepository
{
    public async Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Wallets.FindAsync([id], cancellationToken);

    public async Task<Wallet?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
        await context.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.PhoneNumber == phoneNumber, cancellationToken);

    public async Task<IReadOnlyList<Wallet>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
        await context.Wallets
            .AsNoTracking()
            .Where(w => w.OwnerId == ownerId && w.OwnerId != SystemConstants.SystemUserId)
            .ToListAsync(cancellationToken);

    public async Task<int> CountByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
        await context.Wallets
            .CountAsync(w => w.OwnerId == ownerId && w.OwnerId != SystemConstants.SystemUserId, cancellationToken);

    public void Add(Wallet wallet) => context.Wallets.Add(wallet);

    public void Update(Wallet wallet) => context.Wallets.Update(wallet);
}
