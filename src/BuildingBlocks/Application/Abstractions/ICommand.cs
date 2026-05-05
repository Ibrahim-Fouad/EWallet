using EWallet.BuildingBlocks.Common;
using MediatR;

namespace EWallet.BuildingBlocks.Application.Abstractions;

public interface ICommand : IRequest<Result>;
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
