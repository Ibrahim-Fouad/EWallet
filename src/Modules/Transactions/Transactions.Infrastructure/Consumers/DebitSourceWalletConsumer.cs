using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.Modules.Transactions.Application.Sagas;
using EWallet.Modules.Transactions.Domain.Repositories;
using EWallet.Modules.Wallets.Domain.Repositories;
using MassTransit;

namespace EWallet.Modules.Transactions.Infrastructure.Consumers;

/// <summary>
/// Processes DebitWalletCommand published by the saga (Debiting state).
///
/// On failure, marks the Transaction as Failed and publishes DebitFailedEvent.
/// Transaction.Fail() is committed atomically with the DebitFailedEvent outbox
/// message by the EF outbox middleware's TransactionsDbContext.SaveChangesAsync().
///
/// On success, publishes WalletDebitedEvent. The wallet save (WalletsDbContext) and
/// the event publish (TransactionsDbContext outbox) use two separate DbContexts;
/// a narrow crash window exists between them — MassTransit at-least-once retry covers
/// transient failures.
/// </summary>
public sealed class DebitSourceWalletConsumer(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    ITransactionRepository transactionRepository)
    : IConsumer<DebitWalletCommand>
{
    public async Task Consume(ConsumeContext<DebitWalletCommand> context)
    {
        var wallet = await walletRepository.GetByIdAsync(
            context.Message.WalletId, context.CancellationToken);

        if (wallet is null)
        {
            var tx = await transactionRepository.GetByIdAsync(
                context.Message.TransactionId, context.CancellationToken);
            tx?.Fail("Source wallet not found.");

            await context.Publish(new DebitFailedEvent(
                context.Message.CorrelationId,
                context.Message.TransactionId,
                "Source wallet not found."));
            return;
        }

        var result = wallet.Debit(context.Message.Amount);
        if (result.IsFailure)
        {
            var tx = await transactionRepository.GetByIdAsync(
                context.Message.TransactionId, context.CancellationToken);
            tx?.Fail(result.Error.Description);

            await context.Publish(new DebitFailedEvent(
                context.Message.CorrelationId,
                context.Message.TransactionId,
                result.Error.Description));
            return;
        }

        // Throws DbUpdateConcurrencyException on optimistic concurrency conflict;
        // MassTransit will retry the consumer automatically.
        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        await context.Publish(new WalletDebitedEvent(
            context.Message.CorrelationId,
            context.Message.TransactionId));
    }
}
