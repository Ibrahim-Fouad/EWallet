using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.Modules.Identity.Application.Abstractions;
using EWallet.Modules.Merchants.Application.Abstractions;
using EWallet.Modules.Merchants.Domain.Errors;
using EWallet.Modules.Merchants.Domain.Repositories;

namespace EWallet.Modules.Merchants.Application.Commands.SuspendMerchant;

internal sealed class SuspendMerchantCommandHandler(
    IMerchantRepository merchantRepository,
    IMerchantUnitOfWork unitOfWork,
    IMerchantOAuthService merchantOAuthService)
    : ICommandHandler<SuspendMerchantCommand>
{
    public async Task<Result> Handle(
        SuspendMerchantCommand request,
        CancellationToken cancellationToken)
    {
        var merchant = await merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
        if (merchant is null)
            return Result.Failure(MerchantErrors.NotFound);

        merchant.Suspend(request.AdminUserId);

        await merchantOAuthService.DisableClientAsync(merchant.Id, cancellationToken);
        await merchantOAuthService.RevokeAllTokensAsync(merchant.Id, cancellationToken);

        await unitOfWork.DispatchDomainEventsAsync(cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
