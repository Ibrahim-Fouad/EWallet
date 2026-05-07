using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Transactions.Application.Sagas;
using MassTransit;

namespace EWallet.Modules.Transactions.Infrastructure.Sagas;

/// <summary>
/// Orchestrates the money transfer flow as a durable, compensating saga.
///
/// Transaction.Status updates are performed by the consumers themselves,
/// not by saga activities, so the status change and the result event are
/// committed atomically via the EF outbox middleware in a single SaveChangesAsync.
///
/// State transitions:
///   Initial
///     [TransferRequested] -> Debiting   : send DebitWalletCommand to source wallet consumer
///
///   Debiting
///     [WalletDebited]    -> Crediting   : send CreditWalletCommand to destination wallet consumer
///     [DebitFailed]      -> Failed      : consumer set Status=Failed before publishing this event
///
///   Crediting
///     [WalletCredited]   -> Completed   : publish TransferCompletedEvent (cross-module)
///                                         consumer set Status=Completed before publishing WalletCreditedEvent
///     [CreditFailed]     -> Compensating: send ReverseDebitCommand (return funds to source wallet)
///
///   Compensating
///     [DebitReversed]    -> Failed      : consumer set Status=Failed before publishing this event
/// </summary>
public sealed class TransferSagaStateMachine : MassTransitStateMachine<TransferSagaState>
{
    public State Debiting { get; private set; } = null!;
    public State Crediting { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;
    public State Compensating { get; private set; } = null!;

    public Event<TransferRequestedMessage> TransferRequested { get; private set; } = null!;
    public Event<WalletDebitedEvent> WalletDebited { get; private set; } = null!;
    public Event<WalletCreditedEvent> WalletCredited { get; private set; } = null!;
    public Event<DebitFailedEvent> DebitFailed { get; private set; } = null!;
    public Event<CreditFailedEvent> CreditFailed { get; private set; } = null!;
    public Event<DebitReversedEvent> DebitReversed { get; private set; } = null!;

    public TransferSagaStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => TransferRequested, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => WalletDebited,     x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => WalletCredited,    x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => DebitFailed,       x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => CreditFailed,      x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => DebitReversed,     x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

        // Initial -> Debiting
        Initially(
            When(TransferRequested)
                .Then(ctx =>
                {
                    ctx.Saga.TransactionId       = ctx.Message.TransactionId;
                    ctx.Saga.SourceWalletId      = ctx.Message.SourceWalletId;
                    ctx.Saga.DestinationWalletId = ctx.Message.DestinationWalletId;
                    ctx.Saga.Amount              = ctx.Message.Amount;
                    ctx.Saga.Currency            = ctx.Message.Currency;
                })
                .Publish(ctx => new DebitWalletCommand(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.TransactionId,
                    ctx.Saga.SourceWalletId,
                    ctx.Saga.Amount))
                .TransitionTo(Debiting));

        // Debiting -> Crediting | Failed
        During(Debiting,
            When(WalletDebited)
                .Publish(ctx => new CreditWalletCommand(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.TransactionId,
                    ctx.Saga.DestinationWalletId,
                    ctx.Saga.Amount))
                .TransitionTo(Crediting),

            When(DebitFailed)
                .Then(ctx => ctx.Saga.FailureReason = ctx.Message.Reason)
                .TransitionTo(Failed)
                .Finalize());

        // Crediting -> Completed | Compensating
        During(Crediting,
            When(WalletCredited)
                .Publish(ctx => new TransferCompletedEvent(
                    ctx.Saga.TransactionId,
                    ctx.Saga.SourceWalletId,
                    ctx.Saga.DestinationWalletId,
                    ctx.Saga.Amount,
                    ctx.Saga.Currency,
                    DateTimeOffset.UtcNow))
                .TransitionTo(Completed)
                .Finalize(),

            When(CreditFailed)
                .Then(ctx => ctx.Saga.FailureReason = ctx.Message.Reason)
                .Publish(ctx => new ReverseDebitCommand(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.TransactionId,
                    ctx.Saga.SourceWalletId,
                    ctx.Saga.Amount,
                    ctx.Saga.FailureReason))
                .TransitionTo(Compensating));

        // Compensating -> Failed
        During(Compensating,
            When(DebitReversed)
                .TransitionTo(Failed)
                .Finalize());

        // Remove finalized saga instances from the DB
        SetCompletedWhenFinalized();
    }
}
