using EWallet.BuildingBlocks.Domain.Abstractions;

namespace EWallet.Modules.Wallets.Domain.Events;

public sealed record FundsDepositedEvent(Guid WalletId, decimal Amount) : IDomainEvent;
