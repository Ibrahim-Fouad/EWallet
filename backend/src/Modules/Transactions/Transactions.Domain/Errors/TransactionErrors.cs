using EWallet.BuildingBlocks.Common;

namespace EWallet.Modules.Transactions.Domain.Errors;

public static class TransactionErrors
{
    public static readonly Error SelfTransferNotAllowed =
        Error.Validation("Transaction.SelfTransfer", "Cannot transfer to the same wallet.");

    public static readonly Error CurrencyMismatch =
        Error.Validation("Transaction.CurrencyMismatch", "Source and destination wallets must have the same currency.");

    public static readonly Error InvalidAmount =
        Error.Validation("Transaction.InvalidAmount", "Transfer amount must be greater than zero.");

    public static readonly Error WalletLocked =
        Error.Conflict("Transaction.WalletLocked", "Wallet is currently locked by another operation. Please retry.");

    public static readonly Error ConcurrencyConflict =
        Error.Conflict("Transaction.ConcurrencyConflict", "Concurrent modification detected. Please retry with the same idempotency key.");

    public static readonly Error TransactionNotFound =
        Error.NotFound("Transaction.NotFound", "The requested transaction was not found.");

    public static readonly Error DestinationWalletNotFound =
        Error.NotFound("Transaction.DestinationNotFound", "Destination wallet not found for the given phone number.");

    public static readonly Error SourceWalletNotFound =
        Error.NotFound("Transaction.SourceNotFound", "Source wallet not found.");
}
