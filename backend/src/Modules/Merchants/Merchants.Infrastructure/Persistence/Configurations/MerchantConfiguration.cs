using EWallet.Modules.Merchants.Domain.Entities;
using EWallet.Modules.Merchants.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EWallet.Modules.Merchants.Infrastructure.Persistence.Configurations;

internal sealed class MerchantConfiguration : IEntityTypeConfiguration<Merchant>
{
    public void Configure(EntityTypeBuilder<Merchant> builder)
    {
        builder.ToTable("merchants", "merchants");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.BusinessName).IsRequired().HasMaxLength(200);
        builder.Property(m => m.OwnerUserId).IsRequired();
        builder.Property(m => m.ReceivingWalletId).IsRequired();
        builder.Property(m => m.Currency).IsRequired().HasMaxLength(10);
        builder.Property(m => m.CallbackUrl).IsRequired().HasMaxLength(2000);

        builder.Property(m => m.WebhookSecretEncrypted);

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(m => m.OpenIddictClientId).HasMaxLength(100);
        builder.HasIndex(m => m.OpenIddictClientId).IsUnique().HasFilter("[OpenIddictClientId] IS NOT NULL");

        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.ApprovedAt);
        builder.Property(m => m.ApprovedBy);
    }
}
