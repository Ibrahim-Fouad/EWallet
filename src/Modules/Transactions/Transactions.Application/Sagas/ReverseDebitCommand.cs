namespace EWallet.Modules.Transactions.Application.Sagas;

/// <summary>
/// Published by the saga during compensation (credit failed) to reverse the debit
/// on the source wallet.  FailureReason is forwarded so ReverseDebitConsumer can
/// persist it on the Transaction without needing to read the saga state.
/// </summary>
public sealed record ReverseDebitCommand(
    Guid CorrelationId,
    Guid TransactionId,
    Guid WalletId,
    decimal Amount,
    string? FailureReason);
