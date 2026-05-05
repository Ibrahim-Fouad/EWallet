using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.Modules.Wallets.Domain.Enums;

namespace EWallet.Modules.Wallets.Application.Commands.CreateWallet;

public sealed record CreateWalletCommand(Guid OwnerId, string PhoneNumber, Currency Currency)
    : ICommand<CreateWalletResponse>;

public sealed record CreateWalletResponse(Guid WalletId, string PhoneNumber, string Currency, decimal Balance);
