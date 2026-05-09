using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.Modules.Wallets.Application.DTOs;
using EWallet.Modules.Wallets.Domain.Errors;
using EWallet.Modules.Wallets.Domain.Repositories;

namespace EWallet.Modules.Wallets.Application.Queries.GetWalletById;

internal sealed class GetWalletByIdQueryHandler(IWalletRepository walletRepository)
    : IQueryHandler<GetWalletByIdQuery, WalletDto>
{
    public async Task<Result<WalletDto>> Handle(
        GetWalletByIdQuery request,
        CancellationToken cancellationToken)
    {
        var wallet = await walletRepository.GetByIdAsync(request.WalletId, cancellationToken);
        if (wallet is null)
            return Result.Failure<WalletDto>(WalletErrors.WalletNotFound);

        if (wallet.OwnerId != request.RequestingUserId)
            return Result.Failure<WalletDto>(
                Error.Unauthorized("Wallet.Unauthorized", "You do not own this wallet."));

        return Result.Success(new WalletDto(
            wallet.Id,
            wallet.OwnerId,
            wallet.PhoneNumber,
            wallet.Balance,
            wallet.Currency.ToString(),
            wallet.IsActive,
            wallet.CreatedAt));
    }
}
