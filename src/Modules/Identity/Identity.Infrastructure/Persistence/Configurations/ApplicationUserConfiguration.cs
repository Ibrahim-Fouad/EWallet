using EWallet.BuildingBlocks.Common.Constants;
using EWallet.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EWallet.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.IsSystem).HasDefaultValue(false);
        builder.Property(u => u.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        // NationalId — new custom column, required, unique
        builder.Property(u => u.NationalId)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(u => u.NationalId)
            .IsUnique()
            .HasDatabaseName("IX_AspNetUsers_NationalId");

        // PhoneNumber — inherited from IdentityUser, promoted to required + unique
        builder.Property(u => u.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(u => u.PhoneNumber)
            .IsUnique()
            .HasDatabaseName("IX_AspNetUsers_PhoneNumber");

        // System user seed — placeholder values satisfy non-null DB constraint.
        // The system user is never exposed via any endpoint or registration form.
        builder.HasData(new ApplicationUser
        {
            Id                 = SystemConstants.SystemUserId,
            UserName           = "system@ewallet.internal",
            NormalizedUserName = "SYSTEM@EWALLET.INTERNAL",
            Email              = "system@ewallet.internal",
            NormalizedEmail    = "SYSTEM@EWALLET.INTERNAL",
            IsSystem           = true,
            CreatedAt          = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            SecurityStamp      = "SYSTEM_SECURITY_STAMP",
            ConcurrencyStamp   = "SYSTEM_CONCURRENCY_STAMP",
            NationalId         = "SYSTEM",
            PhoneNumber        = "SYSTEM",
        });
    }
}
