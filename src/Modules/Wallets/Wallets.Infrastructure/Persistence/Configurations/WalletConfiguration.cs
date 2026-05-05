using EWallet.BuildingBlocks.Common.Constants;
using EWallet.Modules.Wallets.Domain.Entities;
using EWallet.Modules.Wallets.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EWallet.Modules.Wallets.Infrastructure.Persistence.Configurations;

internal sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("wallets", "wallets");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.OwnerId).IsRequired();
        builder.Property(w => w.PhoneNumber).IsRequired().HasMaxLength(20);
        builder.HasIndex(w => w.PhoneNumber).IsUnique();

        builder.Property(w => w.Balance)
            .IsRequired()
            .HasColumnType("decimal(18,4)");

        builder.Property(w => w.Currency)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(3);

        builder.Property(w => w.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(w => w.CreatedAt).IsRequired();

        builder.Property(w => w.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Seed system wallets
        var now = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(
            new
            {
                Id = SystemConstants.SystemWalletEgpId,
                OwnerId = SystemConstants.SystemUserId,
                PhoneNumber = SystemConstants.SystemPhoneEgp,
                Balance = 1_000_000m,
                Currency = Currency.EGP,
                IsActive = true,
                CreatedAt = now,
                RowVersion = Array.Empty<byte>()
            },
            new
            {
                Id = SystemConstants.SystemWalletUsdId,
                OwnerId = SystemConstants.SystemUserId,
                PhoneNumber = SystemConstants.SystemPhoneUsd,
                Balance = 1_000_000m,
                Currency = Currency.USD,
                IsActive = true,
                CreatedAt = now,
                RowVersion = Array.Empty<byte>()
            });
    }
}
