using EWallet.BuildingBlocks.Common;

namespace EWallet.Modules.Wallets.Domain.Errors;

public static class WalletErrors
{
    public static readonly Error MaxWalletsReached =
        Error.Validation("Wallet.MaxWalletsReached", "A user can own at most 3 wallets.");

    public static readonly Error PhoneNumberAlreadyInUse =
        Error.Conflict("Wallet.PhoneNumberAlreadyInUse", "This phone number is already registered to another wallet.");

    public static readonly Error WalletNotFound =
        Error.NotFound("Wallet.NotFound", "The requested wallet was not found.");

    public static readonly Error InsufficientFunds =
        Error.Validation("Wallet.InsufficientFunds", "Insufficient funds to complete the transaction.");

    public static readonly Error InvalidAmount =
        Error.Validation("Wallet.InvalidAmount", "Amount must be greater than zero.");

    public static readonly Error WalletInactive =
        Error.Validation("Wallet.Inactive", "This wallet is inactive.");
}
