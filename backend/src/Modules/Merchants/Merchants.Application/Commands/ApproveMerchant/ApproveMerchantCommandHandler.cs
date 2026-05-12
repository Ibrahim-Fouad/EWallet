using System.Security.Cryptography;
using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.Modules.Identity.Application.Abstractions;
using EWallet.Modules.Merchants.Application.Abstractions;
using EWallet.Modules.Merchants.Domain.Errors;
using EWallet.Modules.Merchants.Domain.Repositories;
using Microsoft.AspNetCore.DataProtection;

namespace EWallet.Modules.Merchants.Application.Commands.ApproveMerchant;

internal sealed class ApproveMerchantCommandHandler(
    IMerchantRepository merchantRepository,
    IMerchantUnitOfWork unitOfWork,
    IMerchantOAuthService merchantOAuthService,
    IDataProtectionProvider dataProtectionProvider)
    : ICommandHandler<ApproveMerchantCommand, ApproveMerchantResponse>
{
    private const string ProtectorPurpose = "Merchants.WebhookSecret";

    public async Task<Result<ApproveMerchantResponse>> Handle(
        ApproveMerchantCommand request,
        CancellationToken cancellationToken)
    {
        var merchant = await merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
        if (merchant is null)
            return Result.Failure<ApproveMerchantResponse>(MerchantErrors.NotFound);

        var clientSecretBytes = RandomNumberGenerator.GetBytes(32);
        var webhookSecretBytes = RandomNumberGenerator.GetBytes(32);

        var protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        var webhookSecretEncrypted = protector.Protect(webhookSecretBytes);

        var clientId = await merchantOAuthService.CreateClientAsync(
            merchant.Id,
            Convert.ToBase64String(clientSecretBytes),
            cancellationToken);

        merchant.Approve(request.AdminUserId, webhookSecretEncrypted, clientId);

        await unitOfWork.DispatchDomainEventsAsync(cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new ApproveMerchantResponse(
            clientId,
            Convert.ToBase64String(clientSecretBytes),
            Convert.ToBase64String(webhookSecretBytes)));
    }
}
