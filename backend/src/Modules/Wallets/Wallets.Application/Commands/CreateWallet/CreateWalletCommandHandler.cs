using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.Modules.Wallets.Domain.Entities;
using EWallet.Modules.Wallets.Domain.Errors;
using EWallet.Modules.Wallets.Domain.Repositories;

namespace EWallet.Modules.Wallets.Application.Commands.CreateWallet;

internal sealed class CreateWalletCommandHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateWalletCommand, CreateWalletResponse>
{
    private const int MaxWalletsPerUser = 3;

    public async Task<Result<CreateWalletResponse>> Handle(
        CreateWalletCommand request,
        CancellationToken cancellationToken)
    {
        var walletCount = await walletRepository.CountByOwnerIdAsync(request.OwnerId, cancellationToken);
        if (walletCount >= MaxWalletsPerUser)
            return Result.Failure<CreateWalletResponse>(WalletErrors.MaxWalletsReached);

        var existingPhone = await walletRepository.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
        if (existingPhone is not null)
            return Result.Failure<CreateWalletResponse>(WalletErrors.PhoneNumberAlreadyInUse);

        var walletResult = Wallet.Create(request.OwnerId, request.PhoneNumber, request.Currency);
        if (walletResult.IsFailure)
            return Result.Failure<CreateWalletResponse>(walletResult.Error);

        walletRepository.Add(walletResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await unitOfWork.DispatchDomainEventsAsync(cancellationToken);

        var wallet = walletResult.Value;
        return Result.Success(new CreateWalletResponse(
            wallet.Id,
            wallet.PhoneNumber,
            wallet.Currency.ToString(),
            wallet.Balance));
    }
}
