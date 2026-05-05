using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.Modules.Transactions.Application.Sagas;
using EWallet.Modules.Wallets.Domain.Repositories;
using MassTransit;

namespace EWallet.Modules.Transactions.Infrastructure.Consumers;

/// <summary>
/// Compensation consumer: reverses a previously applied debit by crediting the
/// source wallet back the same amount.  Triggered by the saga when CreditFailed
/// (Compensating state).
/// </summary>
public sealed class ReverseDebitConsumer(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork)
    : IConsumer<ReverseDebitCommand>
{
    public async Task Consume(ConsumeContext<ReverseDebitCommand> context)
    {
        var wallet = await walletRepository.GetByIdAsync(
            context.Message.WalletId, context.CancellationToken);

        if (wallet is null)
        {
            // Edge case: wallet disappeared during compensation.
            // Still publish DebitReversedEvent so the saga can finalize to Failed.
            // Manual reconciliation will be required for the missing wallet.
            await context.Publish(new DebitReversedEvent(
                context.Message.CorrelationId,
                context.Message.TransactionId));
            return;
        }

        // Credit reversal: return the debited amount to the source wallet
        wallet.Credit(context.Message.Amount);
        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        await context.Publish(new DebitReversedEvent(
            context.Message.CorrelationId,
            context.Message.TransactionId));
    }
}
