namespace EWallet.Modules.Transactions.Application.Sagas;

/// <summary>
/// Published by TransferCommandHandler (via IEventBus / EF outbox) to start the saga.
/// CorrelationId == TransactionId — one saga instance per transfer attempt.
/// </summary>
public sealed record TransferRequestedMessage(
    Guid CorrelationId,
    Guid TransactionId,
    Guid SourceWalletId,
    Guid DestinationWalletId,
    decimal Amount,
    string Currency);
