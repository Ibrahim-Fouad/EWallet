using EWallet.Modules.Merchants.Domain.Entities;
using EWallet.Modules.Merchants.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EWallet.Modules.Merchants.Infrastructure.Persistence.Repositories;

internal sealed class MerchantRepository(MerchantsDbContext context) : IMerchantRepository
{
    public void Add(Merchant merchant) => context.Merchants.Add(merchant);

    public async Task<Merchant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Merchants.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<Merchant?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default) =>
        await context.Merchants.FirstOrDefaultAsync(m => m.OpenIddictClientId == clientId, cancellationToken);
}
