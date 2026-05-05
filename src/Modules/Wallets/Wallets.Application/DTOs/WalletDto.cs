namespace EWallet.Modules.Wallets.Application.DTOs;

public sealed record WalletDto(
    Guid Id,
    Guid OwnerId,
    string PhoneNumber,
    decimal Balance,
    string Currency,
    bool IsActive,
    DateTimeOffset CreatedAt);
