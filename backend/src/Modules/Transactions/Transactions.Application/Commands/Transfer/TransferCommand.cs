using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;

namespace EWallet.Modules.Transactions.Application.Commands.Transfer;

public sealed record TransferCommand(
    string IdempotencyKey,
    string SourcePhoneNumber,
    string DestinationPhoneNumber,
    decimal Amount,
    Guid RequestingUserId,
    string? Notes = null,
    string? DestinationDisplayOverride = null,
    string? DescriptionOverride = null,
    string? Origin = null) : ICommand<TransferResponse>;

public sealed record TransferResponse(
    Guid TransactionId,
    string Status,
    decimal Amount,
    string Currency);
