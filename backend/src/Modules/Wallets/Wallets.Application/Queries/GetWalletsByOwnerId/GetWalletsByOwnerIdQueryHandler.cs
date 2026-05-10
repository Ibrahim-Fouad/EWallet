using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.Modules.Wallets.Application.DTOs;
using EWallet.Modules.Wallets.Domain.Repositories;

namespace EWallet.Modules.Wallets.Application.Queries.GetWalletsByOwnerId;

internal sealed class GetWalletsByOwnerIdQueryHandler(IWalletRepository walletRepository)
    : IQueryHandler<GetWalletsByOwnerIdQuery, IReadOnlyList<WalletDto>>
{
    public async Task<Result<IReadOnlyList<WalletDto>>> Handle(
        GetWalletsByOwnerIdQuery request,
        CancellationToken cancellationToken)
    {
        var wallets = await walletRepository.GetByOwnerIdAsync(request.OwnerId, cancellationToken);
        var dtos = wallets
            .Select(w => new WalletDto(w.Id, w.OwnerId, w.PhoneNumber, w.Balance, w.Currency.ToString(), w.IsActive, w.CreatedAt))
            .ToList()
            .AsReadOnly();
        return Result.Success<IReadOnlyList<WalletDto>>(dtos);
    }
}
