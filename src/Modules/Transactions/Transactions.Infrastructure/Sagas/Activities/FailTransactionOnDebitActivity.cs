using EWallet.Modules.Transactions.Domain.Repositories;
using EWallet.Modules.Transactions.Infrastructure.Sagas;
using MassTransit;

namespace EWallet.Modules.Transactions.Infrastructure.Sagas.Activities;

/// <summary>
/// Marks the Transaction as Failed after DebitFailed (Debiting → Failed).
/// No compensation needed — the source wallet was never touched.
///
/// FailureReason is read from context.Saga because .Then() copies
/// context.Message.Reason into context.Saga.FailureReason before this activity runs.
/// </summary>
public sealed class FailTransactionOnDebitActivity(ITransactionRepository transactionRepository)
    : IStateMachineActivity<TransferSagaState>
{
    public async Task Execute(
        BehaviorContext<TransferSagaState> context,
        IBehavior<TransferSagaState> next)
        => await next.Execute(context);

    public async Task Execute<T>(
        BehaviorContext<TransferSagaState, T> context,
        IBehavior<TransferSagaState, T> next)
        where T : class
    {
        var transaction = await transactionRepository.GetByIdAsync(
            context.Saga.TransactionId, context.CancellationToken);

        transaction?.Fail(context.Saga.FailureReason);

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

    public void Probe(ProbeContext context) => context.CreateScope("fail-transaction-on-debit");
    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}
