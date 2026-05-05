namespace EWallet.Modules.Transactions.Application.DTOs;

public sealed record TransactionDto(
    Guid Id,
    string IdempotencyKey,
    Guid SourceWalletId,
    Guid DestinationWalletId,
    decimal Amount,
    string Currency,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string Description,
    string? Notes);
