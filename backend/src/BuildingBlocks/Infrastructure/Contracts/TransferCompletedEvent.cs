namespace EWallet.BuildingBlocks.Infrastructure.Contracts;

public sealed record TransferCompletedEvent(
    Guid TransactionId,
    Guid SourceWalletId,
    Guid DestinationWalletId,
    decimal Amount,
    string Currency,
    DateTimeOffset CompletedAt,
    string? Origin = null);
