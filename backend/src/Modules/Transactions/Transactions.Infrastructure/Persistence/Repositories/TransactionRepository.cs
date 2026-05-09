using EWallet.BuildingBlocks.Common;
using EWallet.Modules.Transactions.Domain.Entities;
using EWallet.Modules.Transactions.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EWallet.Modules.Transactions.Infrastructure.Persistence.Repositories;

internal sealed class TransactionRepository(TransactionsDbContext context) : ITransactionRepository
{
    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Transactions
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<Transaction?> GetByIdempotencyKeyAsync(string key, CancellationToken cancellationToken = default) =>
        await context.Transactions
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.IdempotencyKey == key, cancellationToken);

    public async Task<PagedResult<Transaction>> GetByWalletIdAsync(
        Guid walletId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Transactions
            .Include(t => t.Entries)
            .Where(t => t.SourceWalletId == walletId || t.DestinationWalletId == walletId)
            .OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Transaction>(items, page, pageSize, totalCount);
    }

    public void Add(Transaction transaction) => context.Transactions.Add(transaction);
}
