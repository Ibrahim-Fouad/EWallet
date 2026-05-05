namespace EWallet.Modules.Transactions.Application.Sagas;

/// <summary>
/// Published by the saga during compensation (credit failed) to reverse the debit
/// on the source wallet.
/// </summary>
public sealed record ReverseDebitCommand(
    Guid CorrelationId,
    Guid TransactionId,
    Guid WalletId,
    decimal Amount);
