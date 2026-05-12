using EWallet.BuildingBlocks.Application.Abstractions;

namespace EWallet.Modules.Merchants.Application.Commands.RegisterMerchant;

public sealed record RegisterMerchantCommand(
    Guid RequestingUserId,
    string BusinessName,
    string ReceivingWalletPhoneNumber,
    string CallbackUrl) : ICommand<RegisterMerchantResponse>;

public sealed record RegisterMerchantResponse(Guid MerchantId, string Status);
