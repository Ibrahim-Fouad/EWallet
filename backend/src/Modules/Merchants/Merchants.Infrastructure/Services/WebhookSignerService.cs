using EWallet.BuildingBlocks.Common.Hmac;
using EWallet.Modules.Merchants.Application.Abstractions;

namespace EWallet.Modules.Merchants.Infrastructure.Services;

internal sealed class WebhookSignerService : IWebhookSigner
{
    public string Sign(string payload, byte[] secret) => WebhookSigner.Sign(payload, secret);
}
