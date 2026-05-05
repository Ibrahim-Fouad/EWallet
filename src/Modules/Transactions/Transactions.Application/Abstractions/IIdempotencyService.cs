using EWallet.Modules.Transactions.Application.Commands.Transfer;

namespace EWallet.Modules.Transactions.Application.Abstractions;

public interface IIdempotencyService
{
    Task<TransferResponse?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task SetAsync(string key, TransferResponse response, CancellationToken cancellationToken = default);
}
