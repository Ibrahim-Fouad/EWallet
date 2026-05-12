using EWallet.BuildingBlocks.Application.Abstractions;

namespace EWallet.Modules.Merchants.Application.Queries.GetPaymentRequestById;

public sealed record GetPaymentRequestByIdQuery(
    Guid PaymentRequestId,
    Guid RequestingUserId,
    bool IsAdmin = false,
    bool IsMerchant = false,
    Guid? MerchantId = null) : IQuery<PaymentRequestDto>;

public sealed record PaymentRequestDto(
    Guid Id,
    Guid MerchantId,
    string CustomerPhoneNumber,
    decimal Amount,
    string Currency,
    string Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ResolvedAt,
    string? FailureReason,
    DateTimeOffset CreatedAt);
