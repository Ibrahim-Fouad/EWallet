namespace EWallet.Modules.Transactions.Application.Sagas;

/// <summary>Published by DebitSourceWalletConsumer on failure (e.g. insufficient funds).</summary>
public sealed record DebitFailedEvent(
    Guid CorrelationId,
    Guid TransactionId,
    string Reason);
