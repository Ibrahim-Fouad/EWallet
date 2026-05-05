using EWallet.BuildingBlocks.Domain.Abstractions;
using EWallet.Modules.Wallets.Domain.Enums;

namespace EWallet.Modules.Wallets.Domain.Events;

public sealed record WalletCreatedEvent(Guid WalletId, Guid OwnerId, Currency Currency) : IDomainEvent;
