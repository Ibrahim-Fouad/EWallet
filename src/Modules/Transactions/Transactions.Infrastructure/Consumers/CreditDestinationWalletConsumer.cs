using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.Modules.Transactions.Application.Sagas;
using EWallet.Modules.Transactions.Domain.Repositories;
using EWallet.Modules.Wallets.Domain.Repositories;
using MassTransit;

namespace EWallet.Modules.Transactions.Infrastructure.Consumers;

/// <summary>
/// Processes CreditWalletCommand published by the saga (Crediting state).
/// On success, marks the Transaction as Completed and publishes WalletCreditedEvent.
/// On failure, publishes CreditFailedEvent so the saga can compensate.
///
/// Transaction.Complete() is committed atomically with the WalletCreditedEvent outbox
/// message by the EF outbox middleware's TransactionsDbContext.SaveChangesAsync() call
/// at the end of this consumer's pipeline — no explicit save needed for the status update.
///
/// The wallet change (WalletsDbContext) is committed separately via unitOfWork.SaveChangesAsync()
/// before the status update. A narrow crash window between these two saves exists; at-least-once
/// retry ensures eventual consistency. Add UseEntityFrameworkOutbox&lt;WalletsDbContext&gt; on this
/// endpoint for strict at-most-once wallet semantics in production.
/// </summary>
public sealed class CreditDestinationWalletConsumer(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    ITransactionRepository transactionRepository)
    : IConsumer<CreditWalletCommand>
{
    public async Task Consume(ConsumeContext<CreditWalletCommand> context)
    {
        var wallet = await walletRepository.GetByIdAsync(
            context.Message.WalletId, context.CancellationToken);

        if (wallet is null)
        {
            await context.Publish(new CreditFailedEvent(
                context.Message.CorrelationId,
                context.Message.TransactionId,
                "Destination wallet not found."));
            return;
        }

        var result = wallet.Credit(context.Message.Amount);
        if (result.IsFailure)
        {
            await context.Publish(new CreditFailedEvent(
                context.Message.CorrelationId,
                context.Message.TransactionId,
                result.Error.Description));
            return;
        }

        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        // Mark the Transaction as Completed. This change is tracked by TransactionsDbContext
        // and committed atomically with the WalletCreditedEvent outbox message below by the
        // EF outbox middleware's final SaveChangesAsync — no extra save call needed here.
        var transaction = await transactionRepository.GetByIdAsync(
            context.Message.TransactionId, context.CancellationToken);
        transaction?.Complete();

        await context.Publish(new WalletCreditedEvent(
            context.Message.CorrelationId,
            context.Message.TransactionId,
            DateTimeOffset.UtcNow));
    }
}
