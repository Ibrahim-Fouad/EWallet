using EWallet.Modules.Transactions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EWallet.Modules.Transactions.Infrastructure.Persistence.Configurations;

internal sealed class TransactionEntryConfiguration : IEntityTypeConfiguration<TransactionEntry>
{
    public void Configure(EntityTypeBuilder<TransactionEntry> builder)
    {
        builder.ToTable("transaction_entries", "transactions");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TransactionId).IsRequired();
        builder.Property(e => e.WalletId).IsRequired();

        builder.Property(e => e.EntryType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(e => e.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,4)");

        builder.Property(e => e.CreatedAt).IsRequired();
    }
}
