namespace EWallet.Modules.Merchants.Application.Jobs;

public interface IDispatchWebhookJob
{
    Task RunAsync(Guid paymentRequestId, int attemptNumber);
}
