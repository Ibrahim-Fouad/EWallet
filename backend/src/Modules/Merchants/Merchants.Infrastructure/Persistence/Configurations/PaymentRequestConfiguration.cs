using EWallet.Modules.Merchants.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EWallet.Modules.Merchants.Infrastructure.Persistence.Configurations;

internal sealed class PaymentRequestConfiguration : IEntityTypeConfiguration<PaymentRequest>
{
    public void Configure(EntityTypeBuilder<PaymentRequest> builder)
    {
        builder.ToTable("payment_requests", "merchants");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.MerchantId).IsRequired();
        builder.Property(r => r.MerchantWalletId).IsRequired();

        builder.Property(r => r.CustomerPhoneNumber).IsRequired().HasMaxLength(20);
        builder.Property(r => r.CustomerWalletId).IsRequired();

        builder.Property(r => r.Amount).IsRequired().HasColumnType("decimal(18,4)");
        builder.Property(r => r.Currency).IsRequired().HasMaxLength(10);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(r => r.Id).IsUnique();

        builder.HasIndex(r => new { r.MerchantId, r.CustomerPhoneNumber })
            .HasFilter("[Status] = 'Pending'")
            .HasDatabaseName("IX_PaymentRequests_MerchantId_Phone_Pending");

        builder.Property(r => r.ExpiresAt).IsRequired();
        builder.Property(r => r.ResolvedAt);
        builder.Property(r => r.FailureReason).HasMaxLength(500);
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.TransferTransactionId);

        builder.Property(r => r.RowVersion).IsRowVersion();
    }
}
