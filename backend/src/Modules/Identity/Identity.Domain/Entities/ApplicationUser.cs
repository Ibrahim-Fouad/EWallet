using Microsoft.AspNetCore.Identity;

namespace EWallet.Modules.Identity.Domain.Entities;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public bool IsSystem { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>National identity number — required and unique across all users.</summary>
    public string NationalId { get; set; } = string.Empty;
}
