using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Transactions.Application.Abstractions;
using EWallet.Modules.Transactions.Application.Sagas;
using EWallet.Modules.Transactions.Domain.Entities;
using EWallet.Modules.Transactions.Domain.Repositories;
using MassTransit;

namespace EWallet.Modules.Transactions.Infrastructure.Consumers;

/// <summary>
/// Receives a WelcomeBonusRequestedIntegrationEvent and creates a proper Transaction
/// record before kicking off the existing transfer saga. This ensures the welcome bonus
/// appears in transaction history with full double-entry bookkeeping, identical to a
/// normal user-initiated transfer.
/// </summary>
public sealed class WelcomeBonusConsumer(
    ITransactionRepository transactionRepository,
    ITransactionUnitOfWork unitOfWork,
    IEventBus eventBus)
    : IConsumer<WelcomeBonusRequestedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<WelcomeBonusRequestedIntegrationEvent> context)
    {
        var msg = context.Message;

        // Idempotency key is deterministic — guarantees exactly one bonus per wallet even
        // if the consumer is retried or the event is delivered more than once.
        var idempotencyKey = $"welcome-bonus-{msg.DestinationWalletId}";

        var existing = await transactionRepository.GetByIdempotencyKeyAsync(
            idempotencyKey, context.CancellationToken);

        if (existing is not null)
            return;

        var description = $"transfer {msg.Amount} from System to {msg.DestinationPhoneNumber}";

        var transaction = Transaction.Create(
            idempotencyKey,
            sourceWalletId: msg.SystemWalletId,
            destinationWalletId: msg.DestinationWalletId,
            amount: msg.Amount,
            currency: msg.Currency,
            description: description,
            notes: null);

        transactionRepository.Add(transaction);
        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        await eventBus.PublishAsync(new TransferRequestedMessage(
            CorrelationId: transaction.Id,
            TransactionId: transaction.Id,
            SourceWalletId: msg.SystemWalletId,
            DestinationWalletId: msg.DestinationWalletId,
            Amount: msg.Amount,
            Currency: msg.Currency), context.CancellationToken);
    }
}
