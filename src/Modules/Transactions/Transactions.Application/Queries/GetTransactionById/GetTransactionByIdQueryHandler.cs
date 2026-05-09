using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Transactions.Application.DTOs;
using EWallet.Modules.Transactions.Domain.Errors;
using EWallet.Modules.Transactions.Domain.Repositories;

namespace EWallet.Modules.Transactions.Application.Queries.GetTransactionById;

internal sealed class GetTransactionByIdQueryHandler(
    ITransactionRepository transactionRepository,
    IWalletLookupService walletLookupService)
    : IQueryHandler<GetTransactionByIdQuery, TransactionDto>
{
    public async Task<Result<TransactionDto>> Handle(
        GetTransactionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.GetByIdAsync(request.TransactionId, cancellationToken);
        if (transaction is null)
            return Result.Failure<TransactionDto>(TransactionErrors.TransactionNotFound);

        // Verify requester owns either the source or destination wallet
        var sourceInfo = await walletLookupService.GetByIdAsync(transaction.SourceWalletId, cancellationToken);
        var destInfo = await walletLookupService.GetByIdAsync(transaction.DestinationWalletId, cancellationToken);

        var sourceOwnerId = sourceInfo.IsSuccess ? sourceInfo.Value.OwnerId : Guid.Empty;
        var destOwnerId = destInfo.IsSuccess ? destInfo.Value.OwnerId : Guid.Empty;

        if (sourceOwnerId != request.RequestingUserId && destOwnerId != request.RequestingUserId)
            return Result.Failure<TransactionDto>(
                Error.Unauthorized("Transaction.Unauthorized", "You are not a participant in this transaction."));

        return Result.Success(new TransactionDto(
            transaction.Id,
            transaction.DestinationPhoneNumber,
            transaction.Amount,
            transaction.Currency,
            transaction.Status.ToString(),
            transaction.CreatedAt,
            transaction.CompletedAt,
            transaction.Description,
            transaction.Notes));
    }
}
