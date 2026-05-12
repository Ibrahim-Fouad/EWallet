using MassTransit;

namespace EWallet.Modules.Transactions.Infrastructure.Sagas;

/// <summary>
/// Persisted state for the TransferSagaStateMachine.
/// CorrelationId == TransactionId (Guid.CreateVersion7()) — one saga instance per transfer.
/// Stored in the transactions schema via TransferSagaStateConfiguration.
/// </summary>
public sealed class TransferSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;

    // Mirror of the Transaction aggregate — kept here so the saga can publish
    // TransferCompletedEvent without a DB round-trip to load the Transaction.
    public Guid TransactionId { get; set; }
    public Guid SourceWalletId { get; set; }
    public Guid DestinationWalletId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;

    /// <summary>Populated when a failure event is received; used by compensation activities.</summary>
    public string? FailureReason { get; set; }

    /// <summary>Nullable for in-flight saga instances at deploy time; treated as Direct when null.</summary>
    public string? Origin { get; set; }
}
