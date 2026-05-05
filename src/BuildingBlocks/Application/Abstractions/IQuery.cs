using EWallet.BuildingBlocks.Common;
using MediatR;

namespace EWallet.BuildingBlocks.Application.Abstractions;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
