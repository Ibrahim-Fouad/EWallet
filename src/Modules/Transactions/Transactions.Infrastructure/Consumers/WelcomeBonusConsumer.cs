using EWallet.BuildingBlocks.Infrastructure.Contracts;
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
///
/// Atomicity: Transaction entity and TransferRequestedMessage outbox record are committed
/// in a single SaveChangesAsync via the EF outbox consumer filter. If either fails the
/// consumer retries from a clean state — the idempotency key prevents duplicate rows.
/// </summary>
public sealed class WelcomeBonusConsumer(ITransactionRepository transactionRepository)
    : IConsumer<WelcomeBonusRequestedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<WelcomeBonusRequestedIntegrationEvent> context)
    {
        var msg = context.Message;

        // Deterministic key — guarantees exactly one bonus transaction per wallet.
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
            destinationPhoneNumber: msg.DestinationPhoneNumber,
            notes: null);

        transactionRepository.Add(transaction);

        // Use context.Publish (not IEventBus) so the EF outbox consumer filter commits
        // the Transaction INSERT and this outbox message in one SaveChangesAsync call.
        // If the commit fails the whole consumer retries atomically with no stuck-Pending row.
        await context.Publish(new TransferRequestedMessage(
            CorrelationId: transaction.Id,
            TransactionId: transaction.Id,
            SourceWalletId: msg.SystemWalletId,
            DestinationWalletId: msg.DestinationWalletId,
            Amount: msg.Amount,
            Currency: msg.Currency), context.CancellationToken);
    }
}
