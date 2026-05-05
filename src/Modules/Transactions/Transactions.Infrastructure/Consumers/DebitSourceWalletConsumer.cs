using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.Modules.Transactions.Application.Sagas;
using EWallet.Modules.Wallets.Domain.Repositories;
using MassTransit;

namespace EWallet.Modules.Transactions.Infrastructure.Consumers;

/// <summary>
/// Processes DebitWalletCommand published by the saga (Debiting state).
///
/// Reliability note: this consumer saves WalletsDbContext and then publishes the
/// result event. Because these use two separate DbContexts (wallets vs. transactions),
/// there is a narrow crash window between the wallet save and the result publish.
/// MassTransit's at-least-once retry covers transient failures; for strict at-most-once
/// semantics in production, add InboxState to WalletsDbContext and configure
/// UseEntityFrameworkOutbox&lt;WalletsDbContext&gt;() on this receive endpoint.
/// </summary>
public sealed class DebitSourceWalletConsumer(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork)
    : IConsumer<DebitWalletCommand>
{
    public async Task Consume(ConsumeContext<DebitWalletCommand> context)
    {
        var wallet = await walletRepository.GetByIdAsync(
            context.Message.WalletId, context.CancellationToken);

        if (wallet is null)
        {
            await context.Publish(new DebitFailedEvent(
                context.Message.CorrelationId,
                context.Message.TransactionId,
                "Source wallet not found."));
            return;
        }

        var result = wallet.Debit(context.Message.Amount);
        if (result.IsFailure)
        {
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
