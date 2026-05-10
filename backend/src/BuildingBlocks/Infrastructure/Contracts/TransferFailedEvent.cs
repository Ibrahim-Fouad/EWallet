namespace EWallet.BuildingBlocks.Infrastructure.Contracts;

public sealed record TransferFailedEvent(
    Guid TransactionId,
    Guid SourceWalletId,
    string FailureReason,
    DateTimeOffset FailedAt);
