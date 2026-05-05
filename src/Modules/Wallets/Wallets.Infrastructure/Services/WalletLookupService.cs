using EWallet.BuildingBlocks.Common;
using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Wallets.Domain.Errors;
using EWallet.Modules.Wallets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EWallet.Modules.Wallets.Infrastructure.Services;

internal sealed class WalletLookupService(WalletsDbContext context) : IWalletLookupService
{
    public async Task<Result<WalletInfo>> GetByPhoneNumberAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        var wallet = await context.Wallets
            .AsNoTracking()
            .Where(w => w.PhoneNumber == phoneNumber)
            .Select(w => new WalletInfo(w.Id, w.OwnerId, w.PhoneNumber, w.Balance, w.Currency.ToString(), w.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return wallet is null
            ? Result.Failure<WalletInfo>(WalletErrors.WalletNotFound)
            : Result.Success(wallet);
    }

    public async Task<Result<WalletInfo>> GetByIdAsync(
        Guid walletId,
        CancellationToken cancellationToken = default)
    {
        var wallet = await context.Wallets
            .AsNoTracking()
            .Where(w => w.Id == walletId)
            .Select(w => new WalletInfo(w.Id, w.OwnerId, w.PhoneNumber, w.Balance, w.Currency.ToString(), w.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return wallet is null
            ? Result.Failure<WalletInfo>(WalletErrors.WalletNotFound)
            : Result.Success(wallet);
    }

    public async Task<Result<IReadOnlyList<WalletInfo>>> GetByOwnerIdAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var wallets = await context.Wallets
            .AsNoTracking()
            .Where(w => w.OwnerId == ownerId)
            .Select(w => new WalletInfo(w.Id, w.OwnerId, w.PhoneNumber, w.Balance, w.Currency.ToString(), w.IsActive))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<WalletInfo>>(wallets);
    }

    public async Task<Result<int>> CountByOwnerIdAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var count = await context.Wallets
            .CountAsync(w => w.OwnerId == ownerId, cancellationToken);
        return Result.Success(count);
    }
}
