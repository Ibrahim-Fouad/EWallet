using System.ComponentModel.DataAnnotations;
using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EWallet.API.Pages.Account;

public sealed class RegisterModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IEventBus eventBus,
    ILogger<RegisterModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public string? ReturnUrl { get; set; }

    public sealed class InputModel
    {
        [Required, EmailAddress, MaxLength(256)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(8), MaxLength(100)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required, MinLength(5), MaxLength(50)]
        [RegularExpression(@"^\d+$", ErrorMessage = "National ID must contain digits only.")]
        [Display(Name = "National ID")]
        public string NationalId { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
            return Page();

        // Uniqueness checks — explicit messages rather than relying on DB constraint errors
        if (await userManager.FindByEmailAsync(Input.Email) is not null)
        {
            ModelState.AddModelError(nameof(Input.Email), "An account with this email already exists.");
            return Page();
        }

        if (await userManager.Users.AnyAsync(u => u.NationalId == Input.NationalId))
        {
            ModelState.AddModelError(nameof(Input.NationalId), "An account with this National ID already exists.");
            return Page();
        }

        if (await userManager.Users.AnyAsync(u => u.PhoneNumber == Input.PhoneNumber))
        {
            ModelState.AddModelError(nameof(Input.PhoneNumber), "An account with this phone number already exists.");
            return Page();
        }

        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = Input.Email,
            Email = Input.Email,
            NationalId = Input.NationalId,
            PhoneNumber = Input.PhoneNumber,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var result = await userManager.CreateAsync(user, Input.Password);

        if (result.Succeeded)
        {
            logger.LogInformation("New user account created: {UserId}", user.Id);

            await eventBus.PublishAsync(
                new UserRegisteredIntegrationEvent(user.Id, user.PhoneNumber!),
                HttpContext.RequestAborted);

            // Sign the user in immediately — the Identity cookie set here is read
            // by /connect/authorize, allowing the PKCE flow to complete without
            // a second login step after registration.
            await signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl);
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return Page();
    }
}
