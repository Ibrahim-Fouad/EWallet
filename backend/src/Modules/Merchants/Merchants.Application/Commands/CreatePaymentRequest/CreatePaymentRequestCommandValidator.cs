using FluentValidation;

namespace EWallet.Modules.Merchants.Application.Commands.CreatePaymentRequest;

public sealed class CreatePaymentRequestCommandValidator : AbstractValidator<CreatePaymentRequestCommand>
{
    public CreatePaymentRequestCommandValidator()
    {
        RuleFor(x => x.CustomerPhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
