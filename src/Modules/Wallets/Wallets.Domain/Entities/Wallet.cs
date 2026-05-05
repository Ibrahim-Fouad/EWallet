using EWallet.BuildingBlocks.Common;
using EWallet.BuildingBlocks.Domain.Abstractions;
using EWallet.Modules.Wallets.Domain.Enums;
using EWallet.Modules.Wallets.Domain.Errors;
using EWallet.Modules.Wallets.Domain.Events;

namespace EWallet.Modules.Wallets.Domain.Entities;

public sealed class Wallet : AggregateRoot
{
    private Wallet() { } // EF Core

    public Guid OwnerId { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    public Currency Currency { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static Result<Wallet> Create(Guid ownerId, string phoneNumber, Currency currency)
    {
        var wallet = new Wallet
        {
            Id = Guid.CreateVersion7(),
            OwnerId = ownerId,
            PhoneNumber = phoneNumber,
            Balance = 0m,
            Currency = currency,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        wallet.RaiseDomainEvent(new WalletCreatedEvent(wallet.Id, ownerId, currency));
        return Result.Success(wallet);
    }

    public Result Deposit(decimal amount)
    {
        if (amount <= 0) return Result.Failure(WalletErrors.InvalidAmount);
        if (!IsActive) return Result.Failure(WalletErrors.WalletInactive);
        Balance += amount;
        RaiseDomainEvent(new FundsDepositedEvent(Id, amount));
        return Result.Success();
    }

    public Result Debit(decimal amount)
    {
        if (amount <= 0) return Result.Failure(WalletErrors.InvalidAmount);
        if (!IsActive) return Result.Failure(WalletErrors.WalletInactive);
        if (Balance < amount) return Result.Failure(WalletErrors.InsufficientFunds);
        Balance -= amount;
        return Result.Success();
    }

    public Result Credit(decimal amount)
    {
        if (amount <= 0) return Result.Failure(WalletErrors.InvalidAmount);
        if (!IsActive) return Result.Failure(WalletErrors.WalletInactive);
        Balance += amount;
        return Result.Success();
    }
}
