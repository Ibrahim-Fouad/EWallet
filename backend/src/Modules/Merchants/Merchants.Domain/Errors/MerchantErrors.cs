using EWallet.BuildingBlocks.Common;

namespace EWallet.Modules.Merchants.Domain.Errors;

public static class MerchantErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Merchant.NotFound", "The requested merchant was not found.");

    public static readonly Error NotActive =
        Error.Validation("Merchant.NotActive", "Merchant is not active.");

    public static readonly Error CallbackUrlInvalid =
        Error.Validation("Merchant.CallbackUrlInvalid", "Callback URL must be a valid absolute HTTP/HTTPS URL.");

    public static readonly Error WalletPhoneMismatch =
        Error.Validation("Merchant.WalletPhoneMismatch", "The phone number does not match the merchant's receiving wallet.");

    public static readonly Error RequestExpired =
        Error.Validation("Merchant.RequestExpired", "The payment request has expired.");

    public static readonly Error RequestNotPending =
        Error.Validation("Merchant.RequestNotPending", "The payment request is not in a pending state.");

    public static readonly Error DuplicatePending =
        Error.Conflict("Merchant.DuplicatePending", "A pending payment request already exists for this customer.");

    public static readonly Error SelfPaymentForbidden =
        Error.Validation("Merchant.SelfPaymentForbidden", "Merchant cannot create a payment request targeting their own wallet.");

    public static readonly Error CurrencyMismatch =
        Error.Validation("Merchant.CurrencyMismatch", "Customer wallet currency does not match merchant wallet currency.");

    public static readonly Error InsufficientBalance =
        Error.Validation("Merchant.InsufficientBalance", "Customer wallet has insufficient balance.");

    public static readonly Error PaymentRequestNotFound =
        Error.NotFound("Merchant.PaymentRequestNotFound", "The requested payment request was not found.");

    public static readonly Error Unauthorized =
        Error.Unauthorized("Merchant.Unauthorized", "You are not authorized to perform this action.");
}
