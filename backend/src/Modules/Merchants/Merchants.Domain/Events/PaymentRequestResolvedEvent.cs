using EWallet.BuildingBlocks.Domain.Abstractions;
using EWallet.Modules.Merchants.Domain.Enums;

namespace EWallet.Modules.Merchants.Domain.Events;

public sealed record PaymentRequestResolvedEvent(
    Guid PaymentRequestId,
    Guid MerchantId,
    PaymentRequestStatus Status,
    DateTimeOffset ResolvedAt) : IDomainEvent;
