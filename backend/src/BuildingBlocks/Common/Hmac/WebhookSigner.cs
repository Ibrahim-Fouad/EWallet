using System.Security.Cryptography;
using System.Text;

namespace EWallet.BuildingBlocks.Common.Hmac;

public static class WebhookSigner
{
    public static string Sign(string payload, byte[] secret)
    {
        using var hmac = new HMACSHA256(secret);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
