using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.Modules.Transactions.Application.Sagas;
using EWallet.Modules.Transactions.Domain.Repositories;
using EWallet.Modules.Wallets.Domain.Repositories;
using MassTransit;

namespace EWallet.Modules.Transactions.Infrastructure.Consumers;

/// <summary>
/// Compensation consumer: reverses a previously applied debit by crediting the
/// source wallet back the same amount, then marks the Transaction as Failed.
///
/// Transaction.Fail() is committed atomically with the DebitReversedEvent outbox
/// message by the EF outbox middleware's TransactionsDbContext.SaveChangesAsync().
/// The FailureReason is forwarded from the saga via ReverseDebitCommand so the
/// consumer can persist it without reading saga state.
/// </summary>
public sealed class ReverseDebitConsumer(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    ITransactionRepository transactionRepository)
    : IConsumer<ReverseDebitCommand>
{
    public async Task Consume(ConsumeContext<ReverseDebitCommand> context)
    {
        var wallet = await walletRepository.GetByIdAsync(
            context.Message.WalletId, context.CancellationToken);

        if (wallet is null)
        {
            // Edge case: wallet disappeared during compensation.
            // Still mark the Transaction as Failed and publish DebitReversedEvent
            // so the saga can finalize. Manual reconciliation required for missing wallet.
            var tx = await transactionRepository.GetByIdAsync(
                context.Message.TransactionId, context.CancellationToken);
            tx?.Fail(context.Message.FailureReason);

            await context.Publish(new DebitReversedEvent(
                context.Message.CorrelationId,
                context.Message.TransactionId));
            return;
        }

        // Credit reversal: return the debited amount to the source wallet
        wallet.Credit(context.Message.Amount);
        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        var transaction = await transactionRepository.GetByIdAsync(
            context.Message.TransactionId, context.CancellationToken);
        transaction?.Fail(context.Message.FailureReason);

        await context.Publish(new DebitReversedEvent(
            context.Message.CorrelationId,
            context.Message.TransactionId));
    }
}
