using EWallet.BuildingBlocks.Domain.Abstractions;

namespace EWallet.Modules.Merchants.Domain.Events;

public sealed record MerchantSuspendedEvent(
    Guid MerchantId,
    Guid SuspendedBy) : IDomainEvent;
