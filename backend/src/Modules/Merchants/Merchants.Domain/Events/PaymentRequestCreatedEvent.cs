using EWallet.BuildingBlocks.Domain.Abstractions;

namespace EWallet.Modules.Merchants.Domain.Events;

public sealed record PaymentRequestCreatedEvent(
    Guid PaymentRequestId,
    Guid MerchantId,
    string CustomerPhoneNumber,
    decimal Amount,
    string Currency,
    DateTimeOffset ExpiresAt) : IDomainEvent;
