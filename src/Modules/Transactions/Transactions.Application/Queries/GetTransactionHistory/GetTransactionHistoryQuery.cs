using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.Modules.Transactions.Application.DTOs;

namespace EWallet.Modules.Transactions.Application.Queries.GetTransactionHistory;

public sealed record GetTransactionHistoryQuery(
    string PhoneNumber,
    Guid RequestingUserId,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<TransactionDto>>;
