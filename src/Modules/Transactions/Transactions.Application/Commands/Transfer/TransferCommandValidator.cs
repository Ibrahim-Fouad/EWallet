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

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage(TransactionErrors.InvalidAmount.Description);

        RuleFor(x => x)
            .Must(x => x.SourceWalletId != x.DestinationWalletId)
            .WithMessage(TransactionErrors.SelfTransferNotAllowed.Description);
    }
}
