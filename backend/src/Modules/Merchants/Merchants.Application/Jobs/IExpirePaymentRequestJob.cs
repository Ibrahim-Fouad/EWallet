namespace EWallet.Modules.Merchants.Application.Jobs;

public interface IExpirePaymentRequestJob
{
    Task RunAsync(Guid requestId);
}
