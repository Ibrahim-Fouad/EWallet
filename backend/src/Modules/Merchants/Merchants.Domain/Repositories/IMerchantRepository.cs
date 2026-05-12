using EWallet.Modules.Merchants.Domain.Entities;

namespace EWallet.Modules.Merchants.Domain.Repositories;

public interface IMerchantRepository
{
    void Add(Merchant merchant);
    Task<Merchant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Merchant?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
}
