namespace EWallet.Modules.Transactions.Application.Sagas;

/// <summary>
/// Published by CreditDestinationWalletConsumer on failure.
/// Triggers compensation: the saga sends ReverseDebitCommand.
/// </summary>
public sealed record CreditFailedEvent(
    Guid CorrelationId,
    Guid TransactionId,
    string Reason);
