using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.Modules.Transactions.Application.DTOs;

namespace EWallet.Modules.Transactions.Application.Queries.GetTransactionById;

public sealed record GetTransactionByIdQuery(Guid TransactionId, Guid RequestingUserId)
    : IQuery<TransactionDto>;
