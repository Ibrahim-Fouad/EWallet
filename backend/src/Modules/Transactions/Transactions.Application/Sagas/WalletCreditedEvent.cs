namespace EWallet.Modules.Transactions.Application.Sagas;

/// <summary>Published by CreditDestinationWalletConsumer on success.</summary>
public sealed record WalletCreditedEvent(
    Guid CorrelationId,
    Guid TransactionId,
    DateTimeOffset CompletedAt);
