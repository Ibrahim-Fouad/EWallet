using EWallet.BuildingBlocks.Domain.Abstractions;
using EWallet.Modules.Transactions.Domain.Enums;
using EWallet.Modules.Transactions.Domain.Events;

namespace EWallet.Modules.Transactions.Domain.Entities;

public sealed class Transaction : AggregateRoot
{
    private readonly List<TransactionEntry> _entries = [];

    private Transaction() { }

    public string IdempotencyKey { get; private set; } = string.Empty;
    public Guid SourceWalletId { get; private set; }
    public Guid DestinationWalletId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public TransactionStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public string DestinationPhoneNumber { get; private set; } = string.Empty;
    public string? FailureReason { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? Notes { get; private set; }

    public IReadOnlyList<TransactionEntry> Entries => _entries.AsReadOnly();

    public static Transaction Create(
        string idempotencyKey,
        Guid sourceWalletId,
        Guid destinationWalletId,
        decimal amount,
        string currency,
        string description,
        string destinationPhoneNumber,
        string? notes = null)
    {
        var transaction = new Transaction
        {
            Id = Guid.CreateVersion7(),
            IdempotencyKey = idempotencyKey,
            SourceWalletId = sourceWalletId,
            DestinationWalletId = destinationWalletId,
            Amount = amount,
            Currency = currency,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            Description = description,
            DestinationPhoneNumber = destinationPhoneNumber,
            Notes = notes
        };

        transaction._entries.Add(TransactionEntry.Create(transaction.Id, sourceWalletId, EntryType.Debit, amount));
        transaction._entries.Add(TransactionEntry.Create(transaction.Id, destinationWalletId, EntryType.Credit, amount));

        transaction.RaiseDomainEvent(new TransferInitiatedEvent(
            transaction.Id,
            sourceWalletId,
            destinationWalletId,
            amount,
            currency));

        return transaction;
    }

    public void Complete()
    {
        Status = TransactionStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Fail(string? reason = null)
    {
        Status = TransactionStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        FailureReason = reason;
    }
}
