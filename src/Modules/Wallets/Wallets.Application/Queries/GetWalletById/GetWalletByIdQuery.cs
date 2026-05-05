using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.Modules.Wallets.Application.DTOs;

namespace EWallet.Modules.Wallets.Application.Queries.GetWalletById;

public sealed record GetWalletByIdQuery(Guid WalletId, Guid RequestingUserId) : IQuery<WalletDto>;
