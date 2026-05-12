using EWallet.Modules.Merchants.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EWallet.Modules.Merchants.Infrastructure.Persistence.Configurations;

internal sealed class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("webhook_deliveries", "merchants");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.PaymentRequestId).IsRequired();
        builder.Property(d => d.MerchantId).IsRequired();
        builder.Property(d => d.AttemptNumber).IsRequired();

        builder.Property(d => d.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(d => d.HangfireJobId).HasMaxLength(100);
        builder.Property(d => d.ResponseStatus);
        builder.Property(d => d.ErrorMessage).HasMaxLength(1000);
        builder.Property(d => d.AttemptedAt);
        builder.Property(d => d.NextRetryAt);
        builder.Property(d => d.CreatedAt).IsRequired();

        builder.HasIndex(d => new { d.PaymentRequestId, d.AttemptNumber });
    }
}
