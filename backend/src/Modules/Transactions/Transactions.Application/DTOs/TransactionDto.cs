namespace EWallet.Modules.Transactions.Application.DTOs;

public sealed record TransactionDto(
    Guid Id,
    string SourcePhoneNumber,
    string DestinationPhoneNumber,
    decimal Amount,
    string Currency,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string Description,
    string? Notes);
