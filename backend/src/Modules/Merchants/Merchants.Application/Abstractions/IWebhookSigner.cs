namespace EWallet.Modules.Merchants.Application.Abstractions;

public interface IWebhookSigner
{
    string Sign(string payload, byte[] secret);
}
