namespace EWallet.Modules.Transactions.Application.Sagas;

/// <summary>Published by DebitSourceWalletConsumer on success.</summary>
public sealed record WalletDebitedEvent(
    Guid CorrelationId,
    Guid TransactionId);
