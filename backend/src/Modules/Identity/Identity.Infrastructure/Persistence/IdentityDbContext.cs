using EWallet.Modules.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;

namespace EWallet.Modules.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("identity");
        builder.UseOpenIddict();
        builder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

        // OpenIddict 5.x defaults the Type column to nvarchar(50), which is too small for
        // the authorization_code token type URI (57 chars). Override to nvarchar(256).
        builder.Entity<OpenIddictEntityFrameworkCoreToken>()
            .Property(t => t.Type)
            .HasMaxLength(256);
    }
}
