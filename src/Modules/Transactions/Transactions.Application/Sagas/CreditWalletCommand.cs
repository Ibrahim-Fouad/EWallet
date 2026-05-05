namespace EWallet.Modules.Transactions.Application.Sagas;

/// <summary>Published by the saga to trigger CreditDestinationWalletConsumer.</summary>
public sealed record CreditWalletCommand(
    Guid CorrelationId,
    Guid TransactionId,
    Guid WalletId,
    decimal Amount);
