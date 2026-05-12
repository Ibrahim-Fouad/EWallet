using EWallet.BuildingBlocks.Domain.Abstractions;
using EWallet.Modules.Merchants.Domain.Enums;

namespace EWallet.Modules.Merchants.Domain.Entities;

public sealed class WebhookDelivery : Entity
{
    private WebhookDelivery() { }

    public Guid PaymentRequestId { get; private set; }
    public Guid MerchantId { get; private set; }
    public int AttemptNumber { get; private set; }
    public WebhookDeliveryStatus Status { get; private set; }
    public string? HangfireJobId { get; private set; }
    public int? ResponseStatus { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset? AttemptedAt { get; private set; }
    public DateTimeOffset? NextRetryAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static WebhookDelivery Create(Guid paymentRequestId, Guid merchantId, int attemptNumber)
    {
        return new WebhookDelivery
        {
            Id = Guid.CreateVersion7(),
            PaymentRequestId = paymentRequestId,
            MerchantId = merchantId,
            AttemptNumber = attemptNumber,
            Status = WebhookDeliveryStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            AttemptedAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkDelivered(int httpStatus)
    {
        Status = WebhookDeliveryStatus.Delivered;
        ResponseStatus = httpStatus;
    }

    public void MarkFailed(int? httpStatus, string? error, DateTimeOffset? nextRetryAt)
    {
        Status = WebhookDeliveryStatus.Failed;
        ResponseStatus = httpStatus;
        ErrorMessage = error;
        NextRetryAt = nextRetryAt;
    }

    public void MarkPermanentlyFailed()
    {
        Status = WebhookDeliveryStatus.CallbackFailed;
    }
}
