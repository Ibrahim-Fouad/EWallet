using EWallet.BuildingBlocks.Domain.Abstractions;

namespace EWallet.Modules.Merchants.Domain.Events;

public sealed record MerchantApprovedEvent(
    Guid MerchantId,
    Guid ApprovedBy) : IDomainEvent;
