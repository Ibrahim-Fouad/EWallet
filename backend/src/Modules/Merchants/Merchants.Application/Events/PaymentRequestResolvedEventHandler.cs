using EWallet.Modules.Merchants.Application.Jobs;
using EWallet.Modules.Merchants.Domain.Events;
using Hangfire;
using MediatR;

namespace EWallet.Modules.Merchants.Application.Events;

internal sealed class PaymentRequestResolvedEventHandler(
    IBackgroundJobClient backgroundJobClient)
    : INotificationHandler<PaymentRequestResolvedEvent>
{
    public Task Handle(PaymentRequestResolvedEvent notification, CancellationToken cancellationToken)
    {
        backgroundJobClient.Enqueue<IDispatchWebhookJob>(
            j => j.RunAsync(notification.PaymentRequestId, 1));

        return Task.CompletedTask;
    }
}
