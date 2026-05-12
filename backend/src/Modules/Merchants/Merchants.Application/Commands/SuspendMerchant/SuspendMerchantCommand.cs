using EWallet.BuildingBlocks.Application.Abstractions;

namespace EWallet.Modules.Merchants.Application.Commands.SuspendMerchant;

public sealed record SuspendMerchantCommand(
    Guid MerchantId,
    Guid AdminUserId) : ICommand;
