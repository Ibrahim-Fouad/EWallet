using EWallet.BuildingBlocks.Domain.Abstractions;

namespace EWallet.Modules.Merchants.Domain.Events;

public sealed record MerchantRegisteredEvent(
    Guid MerchantId,
    Guid OwnerUserId,
    string BusinessName) : IDomainEvent;
