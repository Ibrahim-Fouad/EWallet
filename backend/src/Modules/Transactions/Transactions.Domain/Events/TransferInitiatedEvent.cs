using EWallet.BuildingBlocks.Domain.Abstractions;

namespace EWallet.Modules.Transactions.Domain.Events;

public sealed record TransferInitiatedEvent(
    Guid TransactionId,
    Guid SourceWalletId,
    Guid DestinationWalletId,
    decimal Amount,
    string Currency) : IDomainEvent;
