using EWallet.Modules.Transactions.Application.Abstractions;
using EWallet.Modules.Transactions.Domain.Repositories;
using EWallet.Modules.Transactions.Infrastructure.Sagas;
using MassTransit;

namespace EWallet.Modules.Transactions.Infrastructure.Sagas.Activities;

/// <summary>
/// Marks the Transaction as Failed after compensation completes (Compensating → Failed).
/// The credit step failed, the source wallet debit has been reversed by ReverseDebitConsumer.
///
/// FailureReason is the original credit-failure reason stored in context.Saga.FailureReason
/// when CreditFailedEvent was received (via .Then() in the state machine).
/// </summary>
public sealed class FailTransactionOnCompensationActivity(
    ITransactionRepository transactionRepository,
    ITransactionUnitOfWork unitOfWork)
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

    public void Probe(ProbeContext context) => context.CreateScope("fail-transaction-on-compensation");
    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}
