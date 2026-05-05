using EWallet.BuildingBlocks.Domain.Abstractions;
using EWallet.Modules.Transactions.Domain.Enums;

namespace EWallet.Modules.Transactions.Domain.Entities;

public sealed class TransactionEntry : Entity
{
    private TransactionEntry() { }

    public Guid TransactionId { get; private set; }
    public Guid WalletId { get; private set; }
    public EntryType EntryType { get; private set; }
    public decimal Amount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    internal static TransactionEntry Create(Guid transactionId, Guid walletId, EntryType entryType, decimal amount) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            TransactionId = transactionId,
            WalletId = walletId,
            EntryType = entryType,
            Amount = amount,
            CreatedAt = DateTimeOffset.UtcNow
        };
}
