using EWallet.BuildingBlocks.Application.Abstractions;

namespace EWallet.Modules.Merchants.Application.Commands.ApproveMerchant;

public sealed record ApproveMerchantCommand(
    Guid MerchantId,
    Guid AdminUserId) : ICommand<ApproveMerchantResponse>;

public sealed record ApproveMerchantResponse(string ClientId, string ClientSecret, string WebhookSecret);
