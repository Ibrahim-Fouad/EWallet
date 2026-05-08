using EWallet.Modules.Transactions.Domain.Errors;
using FluentValidation;

namespace EWallet.Modules.Transactions.Application.Commands.Transfer;

public sealed class TransferCommandValidator : AbstractValidator<TransferCommand>
{
    public TransferCommandValidator()
    {
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.SourcePhoneNumber)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.DestinationPhoneNumber)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage(TransactionErrors.InvalidAmount.Description);

        RuleFor(x => x)
            .Must(x => x.SourcePhoneNumber != x.DestinationPhoneNumber)
            .WithMessage(TransactionErrors.SelfTransferNotAllowed.Description);
    }
}
