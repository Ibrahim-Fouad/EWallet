using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.Modules.Wallets.Application.DTOs;

namespace EWallet.Modules.Wallets.Application.Queries.GetWalletsByOwnerId;

public sealed record GetWalletsByOwnerIdQuery(Guid OwnerId) : IQuery<IReadOnlyList<WalletDto>>;
