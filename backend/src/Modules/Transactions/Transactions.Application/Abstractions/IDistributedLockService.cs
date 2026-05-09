namespace EWallet.Modules.Transactions.Application.Abstractions;

public interface IDistributedLockService
{
    Task<IAsyncDisposable?> TryAcquireAsync(string resource, TimeSpan expiry, CancellationToken cancellationToken = default);
}
