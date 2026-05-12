using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.Modules.Merchants.Domain.Errors;
using EWallet.Modules.Merchants.Domain.Repositories;

namespace EWallet.Modules.Merchants.Application.Queries.GetMerchantById;

internal sealed class GetMerchantByIdQueryHandler(
    IMerchantRepository merchantRepository)
    : IQueryHandler<GetMerchantByIdQuery, MerchantDto>
{
    public async Task<Result<MerchantDto>> Handle(
        GetMerchantByIdQuery request,
        CancellationToken cancellationToken)
    {
        var merchant = await merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
        if (merchant is null)
            return Result.Failure<MerchantDto>(MerchantErrors.NotFound);

        return Result.Success(new MerchantDto(
            merchant.Id,
            merchant.BusinessName,
            merchant.OwnerUserId,
            merchant.Currency,
            merchant.CallbackUrl,
            merchant.Status.ToString(),
            merchant.OpenIddictClientId,
            merchant.CreatedAt,
            merchant.ApprovedAt));
    }
}
