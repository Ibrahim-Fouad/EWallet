using FluentValidation;

namespace EWallet.Modules.Merchants.Application.Commands.RegisterMerchant;

public sealed class RegisterMerchantCommandValidator : AbstractValidator<RegisterMerchantCommand>
{
    public RegisterMerchantCommandValidator()
    {
        RuleFor(x => x.BusinessName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ReceivingWalletPhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.CallbackUrl)
            .NotEmpty()
            .MaximumLength(2000)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                         && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("CallbackUrl must be a valid absolute HTTP/HTTPS URL.");
    }
}
