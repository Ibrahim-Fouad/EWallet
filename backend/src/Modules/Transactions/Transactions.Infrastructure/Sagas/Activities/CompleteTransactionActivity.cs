using EWallet.Modules.Transactions.Application.Abstractions;
using EWallet.Modules.Transactions.Domain.Repositories;
using EWallet.Modules.Transactions.Infrastructure.Sagas;
using MassTransit;

namespace EWallet.Modules.Transactions.Infrastructure.Sagas.Activities;

/// <summary>
/// Marks the Transaction aggregate as Completed when the saga transitions
/// Crediting → Completed (triggered by WalletCreditedEvent).
///
/// MassTransit calls Execute&lt;T&gt; (generic) via OfInstanceType; the non-generic
/// Execute is a no-op pass-through for contexts without a message payload.
/// Both run inside the EF outbox pipeline so the Transaction entity change and
/// the TransferCompletedEvent outbox record are committed atomically with the saga state.
/// </summary>
public sealed class CompleteTransactionActivity(
    ITransactionRepository transactionRepository,
    ITransactionUnitOfWork unitOfWork)
    : IStateMachineActivity<TransferSagaState>
{
    public async Task Execute(
        BehaviorContext<TransferSagaState> context,
        IBehavior<TransferSagaState> next)
    {
        var transaction = await transactionRepository.GetByIdAsync(
            context.Saga.TransactionId, context.CancellationToken);

        transaction?.Complete();

        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        await next.Execute(context);
    }
    public async Task Execute<T>(
        BehaviorContext<TransferSagaState, T> context,
        IBehavior<TransferSagaState, T> next)
        where T : class
    {
        var transaction = await transactionRepository.GetByIdAsync(
            context.Saga.TransactionId, context.CancellationToken);

        transaction?.Complete();

        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        await next.Execute(context);
    }

    public async Task Faulted<TException>(
        BehaviorExceptionContext<TransferSagaState, TException> context,
        IBehavior<TransferSagaState> next)
        where TException : Exception
        => await next.Faulted(context);

    public async Task Faulted<T, TException>(
        BehaviorExceptionContext<TransferSagaState, T, TException> context,
        IBehavior<TransferSagaState, T> next)
        where T : class
        where TException : Exception
        => await next.Faulted(context);

    public void Probe(ProbeContext context) => context.CreateScope("complete-transaction");
    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}
