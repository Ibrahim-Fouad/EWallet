using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EWallet.Modules.Transactions.Infrastructure.Sagas;

public sealed class TransferSagaStateConfiguration : IEntityTypeConfiguration<TransferSagaState>
{
    public void Configure(EntityTypeBuilder<TransferSagaState> builder)
    {
        builder.ToTable("transfer_sagas", "transactions");
        builder.HasKey(x => x.CorrelationId);

        builder.Property(x => x.CurrentState)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,4)");

        builder.Property(x => x.FailureReason)
            .HasMaxLength(500);
    }
}
