using EWallet.Modules.Wallets.Domain.Entities;

namespace EWallet.Modules.Wallets.Domain.Repositories;

public interface IWalletRepository
{
    Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Wallet?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Wallet>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<int> CountByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    void Add(Wallet wallet);
    void Update(Wallet wallet);
}
