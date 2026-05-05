using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.Modules.Wallets.Domain.Errors;
using EWallet.Modules.Wallets.Domain.Repositories;

namespace EWallet.Modules.Wallets.Application.Commands.Deposit;

internal sealed class DepositFundsCommandHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DepositFundsCommand, DepositFundsResponse>
{
    public async Task<Result<DepositFundsResponse>> Handle(
        DepositFundsCommand request,
        CancellationToken cancellationToken)
    {
        var wallet = await walletRepository.GetByIdAsync(request.WalletId, cancellationToken);
        if (wallet is null)
            return Result.Failure<DepositFundsResponse>(WalletErrors.WalletNotFound);

        if (wallet.OwnerId != request.RequestingUserId)
            return Result.Failure<DepositFundsResponse>(
                Error.Unauthorized("Wallet.Unauthorized", "You do not own this wallet."));

        var depositResult = wallet.Deposit(request.Amount);
        if (depositResult.IsFailure)
            return Result.Failure<DepositFundsResponse>(depositResult.Error);

        walletRepository.Update(wallet);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new DepositFundsResponse(wallet.Id, wallet.Balance));
    }
}
