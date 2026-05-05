namespace EWallet.Modules.Transactions.Application.Sagas;

/// <summary>Published by the saga to trigger DebitSourceWalletConsumer.</summary>
public sealed record DebitWalletCommand(
    Guid CorrelationId,
    Guid TransactionId,
    Guid WalletId,
    decimal Amount);
