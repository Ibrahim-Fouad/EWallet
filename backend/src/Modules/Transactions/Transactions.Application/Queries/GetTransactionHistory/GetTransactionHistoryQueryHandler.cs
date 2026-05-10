using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Transactions.Application.DTOs;
using EWallet.Modules.Transactions.Domain.Errors;
using EWallet.Modules.Transactions.Domain.Repositories;

namespace EWallet.Modules.Transactions.Application.Queries.GetTransactionHistory;

internal sealed class GetTransactionHistoryQueryHandler(
    ITransactionRepository transactionRepository,
    IWalletLookupService walletLookupService)
    : IQueryHandler<GetTransactionHistoryQuery, PagedResult<TransactionDto>>
{
    public async Task<Result<PagedResult<TransactionDto>>> Handle(
        GetTransactionHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var walletInfo = await walletLookupService.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
        if (walletInfo.IsFailure)
            return Result.Failure<PagedResult<TransactionDto>>(TransactionErrors.SourceWalletNotFound);

        if (walletInfo.Value.OwnerId != request.RequestingUserId)
            return Result.Failure<PagedResult<TransactionDto>>(
                Error.Unauthorized("Transaction.Unauthorized", "You do not own this wallet."));

        var paged = await transactionRepository.GetByWalletIdAsync(
            walletInfo.Value.Id,
            request.Page,
            request.PageSize,
            cancellationToken);

        // Single query to resolve all source wallet phones for the page
        var uniqueSourceIds = paged.Items.Select(t => t.SourceWalletId).Distinct();
        var sourceWallets = await walletLookupService.GetByIdsAsync(uniqueSourceIds, cancellationToken);

        var dtos = paged.Items
            .Select(t => new TransactionDto(
                t.Id,
                sourceWallets.TryGetValue(t.SourceWalletId, out var src) ? src.PhoneNumber : string.Empty,
                t.DestinationPhoneNumber,
                t.Amount,
                t.Currency,
                t.Status.ToString(),
                t.CreatedAt,
                t.CompletedAt,
                t.Description,
                t.Notes))
            .ToList()
            .AsReadOnly();

        return Result.Success(new PagedResult<TransactionDto>(
            dtos,
            paged.Page,
            paged.PageSize,
            paged.TotalCount));
    }
}
