using EWallet.BuildingBlocks.Application.Abstractions;

namespace EWallet.Modules.Wallets.Application.Commands.Deposit;

public sealed record DepositFundsCommand(Guid WalletId, Guid RequestingUserId, decimal Amount)
    : ICommand<DepositFundsResponse>;

public sealed record DepositFundsResponse(Guid WalletId, decimal NewBalance);
