using EWallet.BuildingBlocks.Application.Abstractions;

namespace EWallet.Modules.Merchants.Application.Queries.GetMerchantById;

public sealed record GetMerchantByIdQuery(Guid MerchantId) : IQuery<MerchantDto>;

public sealed record MerchantDto(
    Guid Id,
    string BusinessName,
    Guid OwnerUserId,
    string Currency,
    string CallbackUrl,
    string Status,
    string? OpenIddictClientId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ApprovedAt);
