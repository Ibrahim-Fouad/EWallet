using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Merchants.Application.Abstractions;
using EWallet.Modules.Merchants.Domain.Entities;
using EWallet.Modules.Merchants.Domain.Errors;
using EWallet.Modules.Merchants.Domain.Repositories;

namespace EWallet.Modules.Merchants.Application.Commands.RegisterMerchant;

internal sealed class RegisterMerchantCommandHandler(
    IWalletLookupService walletLookupService,
    IMerchantRepository merchantRepository,
    IMerchantUnitOfWork unitOfWork)
    : ICommandHandler<RegisterMerchantCommand, RegisterMerchantResponse>
{
    public async Task<Result<RegisterMerchantResponse>> Handle(
        RegisterMerchantCommand request,
        CancellationToken cancellationToken)
    {
        var walletResult = await walletLookupService.GetByPhoneNumberAsync(
            request.ReceivingWalletPhoneNumber, cancellationToken);

        if (walletResult.IsFailure)
            return Result.Failure<RegisterMerchantResponse>(MerchantErrors.WalletPhoneMismatch);

        var wallet = walletResult.Value;

        if (wallet.OwnerId != request.RequestingUserId)
            return Result.Failure<RegisterMerchantResponse>(MerchantErrors.Unauthorized);

        if (!wallet.IsActive)
            return Result.Failure<RegisterMerchantResponse>(
                Error.Validation("Merchant.WalletInactive", "The receiving wallet is not active."));

        var merchant = Merchant.Register(
            request.RequestingUserId,
            request.BusinessName,
            wallet.Id,
            wallet.Currency,
            request.CallbackUrl);

        merchantRepository.Add(merchant);

        await unitOfWork.DispatchDomainEventsAsync(cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new RegisterMerchantResponse(merchant.Id, merchant.Status.ToString()));
    }
}
