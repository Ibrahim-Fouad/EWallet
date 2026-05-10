using EWallet.BuildingBlocks.Common;

namespace EWallet.BuildingBlocks.Infrastructure.Contracts;

public sealed record WalletInfo(
    Guid Id,
    Guid OwnerId,
    string PhoneNumber,
    decimal Balance,
    string Currency,
    bool IsActive);

public interface IWalletLookupService
{
    Task<Result<WalletInfo>> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<Result<WalletInfo>> GetByIdAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, WalletInfo>> GetByIdsAsync(IEnumerable<Guid> walletIds, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<WalletInfo>>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<Result<int>> CountByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
}
