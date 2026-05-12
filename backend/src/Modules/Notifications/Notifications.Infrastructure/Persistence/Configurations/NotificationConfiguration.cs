using EWallet.Modules.Notifications.Domain.Entities;
using EWallet.Modules.Notifications.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EWallet.Modules.Notifications.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications", "notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.UserId).IsRequired();
        builder.Property(n => n.TransactionId);  // nullable

        builder.Property(n => n.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(n => n.Amount)
            .HasColumnType("decimal(18,4)");

        builder.Property(n => n.Currency)
            .HasMaxLength(10);

        builder.Property(n => n.SourceWalletId);
        builder.Property(n => n.CompletedAt);
        builder.Property(n => n.ReceivedAt);

        builder.Property(n => n.FailureReason)
            .HasMaxLength(500);

        builder.Property(n => n.PaymentRequestId);

        builder.Property(n => n.MerchantName)
            .HasMaxLength(200);

        builder.Property(n => n.ActionStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(n => n.ActionTakenAt);
        builder.Property(n => n.ExpiresAt);

        builder.Property(n => n.RowVersion)
            .IsRowVersion();

        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.CreatedAt).IsRequired();

        builder.HasIndex(n => new { n.UserId, n.CreatedAt });

        builder.HasIndex(n => new { n.UserId, n.PaymentRequestId })
            .HasFilter("[PaymentRequestId] IS NOT NULL");
    }
}
