using EWallet.BuildingBlocks.Common;
using MediatR;

namespace EWallet.BuildingBlocks.Application.Abstractions;

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
