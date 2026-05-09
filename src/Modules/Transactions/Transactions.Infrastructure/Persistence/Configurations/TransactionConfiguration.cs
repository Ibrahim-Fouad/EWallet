using EWallet.Modules.Transactions.Domain.Entities;
using EWallet.Modules.Transactions.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EWallet.Modules.Transactions.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions", "transactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(128);
        builder.HasIndex(t => t.IdempotencyKey).IsUnique();

        builder.Property(t => t.SourceWalletId).IsRequired();
        builder.Property(t => t.DestinationWalletId).IsRequired();

        builder.Property(t => t.DestinationPhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(t => t.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,4)");

        builder.Property(t => t.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.CompletedAt);

        builder.Property(t => t.FailureReason)
            .HasMaxLength(500);

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.Notes)
            .HasMaxLength(1000);

        builder.HasMany(t => t.Entries)
            .WithOne()
            .HasForeignKey(e => e.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
