namespace EWallet.Modules.Transactions.Application.Sagas;

/// <summary>Published by ReverseDebitConsumer after the compensation debit-reversal succeeds.</summary>
public sealed record DebitReversedEvent(
    Guid CorrelationId,
    Guid TransactionId);
