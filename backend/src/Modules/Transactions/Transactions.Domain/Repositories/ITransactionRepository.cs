using EWallet.BuildingBlocks.Common;
using EWallet.Modules.Transactions.Domain.Entities;

namespace EWallet.Modules.Transactions.Domain.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Transaction?> GetByIdempotencyKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<PagedResult<Transaction>> GetByWalletIdAsync(Guid walletId, int page, int pageSize, CancellationToken cancellationToken = default);
    void Add(Transaction transaction);
}
