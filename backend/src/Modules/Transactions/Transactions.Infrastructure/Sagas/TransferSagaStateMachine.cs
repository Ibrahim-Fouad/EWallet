using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Transactions.Application.Sagas;
using EWallet.Modules.Transactions.Infrastructure.Sagas.Activities;
using MassTransit;

namespace EWallet.Modules.Transactions.Infrastructure.Sagas;

/// <summary>
/// Orchestrates the money transfer flow as a durable, compensating saga.
///
/// State transitions:
///   Initial
///     → [TransferRequested] → Debiting   : send DebitWalletCommand to source wallet consumer
///
///   Debiting
///     → [WalletDebited]    → Crediting   : send CreditWalletCommand to destination wallet consumer
///     → [DebitFailed]      → Failed      : mark Transaction.Failed, publish TransferFailedEvent
///
///   Crediting
///     → [WalletCredited]   → Completed   : mark Transaction.Complete (CompleteTransactionActivity)
///                                          publish TransferCompletedEvent (cross-module, → Notifications)
///     → [CreditFailed]     → Compensating: send ReverseDebitCommand (return funds to source wallet)
///
///   Compensating
///     → [DebitReversed]    → Failed      : mark Transaction.Failed, publish TransferFailedEvent
/// </summary>
public sealed class TransferSagaStateMachine : MassTransitStateMachine<TransferSagaState>
{
    // ── States ────────────────────────────────────────────────────────────────
    public State Debiting { get; private set; } = null!;
    public State Crediting { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;
    public State Compensating { get; private set; } = null!;

    // ── Events ────────────────────────────────────────────────────────────────
    public Event<TransferRequestedMessage> TransferRequested { get; private set; } = null!;
    public Event<WalletDebitedEvent> WalletDebited { get; private set; } = null!;
    public Event<WalletCreditedEvent> WalletCredited { get; private set; } = null!;
    public Event<DebitFailedEvent> DebitFailed { get; private set; } = null!;
    public Event<CreditFailedEvent> CreditFailed { get; private set; } = null!;
    public Event<DebitReversedEvent> DebitReversed { get; private set; } = null!;

    public TransferSagaStateMachine()
    {
        InstanceState(x => x.CurrentState);

        // Correlate all events to the saga instance by CorrelationId
        Event(() => TransferRequested, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => WalletDebited,     x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => WalletCredited,    x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => DebitFailed,       x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => CreditFailed,      x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => DebitReversed,     x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

        // ── Initial → Debiting ────────────────────────────────────────────────
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

        // ── Debiting ──────────────────────────────────────────────────────────
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
                .Activity(x => x.OfInstanceType<FailTransactionOnDebitActivity>())
                .Publish(ctx => new TransferFailedEvent(
                    ctx.Saga.TransactionId,
                    ctx.Saga.SourceWalletId,
                    ctx.Saga.FailureReason ?? "Transfer failed",
                    DateTimeOffset.UtcNow))
                .TransitionTo(Failed)
                .Finalize());

        // ── Crediting ─────────────────────────────────────────────────────────
        During(Crediting,
            When(WalletCredited)
                .Activity(x => x.OfInstanceType<CompleteTransactionActivity>())
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
                    ctx.Saga.Amount))
                .TransitionTo(Compensating));

        // ── Compensating → Failed ─────────────────────────────────────────────
        During(Compensating,
            When(DebitReversed)
                .Activity(x => x.OfInstanceType<FailTransactionOnCompensationActivity>())
                .Publish(ctx => new TransferFailedEvent(
                    ctx.Saga.TransactionId,
                    ctx.Saga.SourceWalletId,
                    ctx.Saga.FailureReason ?? "Transfer failed",
                    DateTimeOffset.UtcNow))
                .TransitionTo(Failed)
                .Finalize());

        // Clean up finalized saga instances from the DB
        SetCompletedWhenFinalized();
    }
}
