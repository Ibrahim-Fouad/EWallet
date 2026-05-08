using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.Modules.Transactions.Application.Sagas;
using EWallet.Modules.Wallets.Domain.Repositories;
using MassTransit;

namespace EWallet.Modules.Transactions.Infrastructure.Consumers;

/// <summary>
/// Processes CreditWalletCommand published by the saga (Crediting state).
/// On success, publishes WalletCreditedEvent which drives the saga to Completed.
/// On failure, publishes CreditFailedEvent which drives the saga to Compensating.
/// </summary>
public sealed class CreditDestinationWalletConsumer(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork)
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

        await context.Publish(new WalletCreditedEvent(
            context.Message.CorrelationId,
            context.Message.TransactionId,
            DateTimeOffset.UtcNow));
    }
}
