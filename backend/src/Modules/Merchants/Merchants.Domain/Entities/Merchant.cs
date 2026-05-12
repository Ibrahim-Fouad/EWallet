using EWallet.BuildingBlocks.Domain.Abstractions;
using EWallet.Modules.Merchants.Domain.Enums;
using EWallet.Modules.Merchants.Domain.Events;

namespace EWallet.Modules.Merchants.Domain.Entities;

public sealed class Merchant : AggregateRoot
{
    private Merchant() { }

    public string BusinessName { get; private set; } = string.Empty;
    public Guid OwnerUserId { get; private set; }
    public Guid ReceivingWalletId { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string CallbackUrl { get; private set; } = string.Empty;
    public byte[]? WebhookSecretEncrypted { get; private set; }
    public MerchantStatus Status { get; private set; }
    public string? OpenIddictClientId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }

    public static Merchant Register(
        Guid ownerUserId,
        string businessName,
        Guid receivingWalletId,
        string currency,
        string callbackUrl)
    {
        var merchant = new Merchant
        {
            Id = Guid.CreateVersion7(),
            BusinessName = businessName,
            OwnerUserId = ownerUserId,
            ReceivingWalletId = receivingWalletId,
            Currency = currency,
            CallbackUrl = callbackUrl,
            Status = MerchantStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        merchant.RaiseDomainEvent(new MerchantRegisteredEvent(merchant.Id, ownerUserId, businessName));
        return merchant;
    }

    public void Approve(Guid adminId, byte[] webhookSecretEncrypted, string clientId)
    {
        Status = MerchantStatus.Active;
        WebhookSecretEncrypted = webhookSecretEncrypted;
        OpenIddictClientId = clientId;
        ApprovedAt = DateTimeOffset.UtcNow;
        ApprovedBy = adminId;
        RaiseDomainEvent(new MerchantApprovedEvent(Id, adminId));
    }

    public void Suspend(Guid adminId)
    {
        Status = MerchantStatus.Suspended;
        RaiseDomainEvent(new MerchantSuspendedEvent(Id, adminId));
    }
}
