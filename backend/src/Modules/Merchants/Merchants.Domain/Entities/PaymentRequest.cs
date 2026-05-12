using EWallet.BuildingBlocks.Domain.Abstractions;
using EWallet.Modules.Merchants.Domain.Enums;
using EWallet.Modules.Merchants.Domain.Events;

namespace EWallet.Modules.Merchants.Domain.Entities;

public sealed class PaymentRequest : AggregateRoot
{
    private PaymentRequest() { }

    public Guid MerchantId { get; private set; }
    public Guid MerchantWalletId { get; private set; }
    public string CustomerPhoneNumber { get; private set; } = string.Empty;
    public Guid CustomerWalletId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public PaymentRequestStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? TransferTransactionId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static PaymentRequest Create(
        Guid merchantId,
        Guid merchantWalletId,
        string customerPhoneNumber,
        Guid customerWalletId,
        decimal amount,
        string currency)
    {
        var now = DateTimeOffset.UtcNow;
        var request = new PaymentRequest
        {
            Id = Guid.CreateVersion7(),
            MerchantId = merchantId,
            MerchantWalletId = merchantWalletId,
            CustomerPhoneNumber = customerPhoneNumber,
            CustomerWalletId = customerWalletId,
            Amount = amount,
            Currency = currency,
            Status = PaymentRequestStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(2)
        };

        request.RaiseDomainEvent(new PaymentRequestCreatedEvent(
            request.Id, merchantId, customerPhoneNumber, amount, currency, request.ExpiresAt));

        return request;
    }

    public void MarkApproved(Guid transferTransactionId)
    {
        Status = PaymentRequestStatus.Approved;
        TransferTransactionId = transferTransactionId;
    }

    public void MarkRejected()
    {
        Status = PaymentRequestStatus.Rejected;
        ResolvedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new PaymentRequestResolvedEvent(Id, MerchantId, Status, ResolvedAt.Value));
    }

    public void MarkExpired()
    {
        if (Status != PaymentRequestStatus.Pending) return;
        Status = PaymentRequestStatus.Expired;
        ResolvedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new PaymentRequestResolvedEvent(Id, MerchantId, Status, ResolvedAt.Value));
    }

    public void MarkCompleted()
    {
        Status = PaymentRequestStatus.Completed;
        ResolvedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new PaymentRequestResolvedEvent(Id, MerchantId, Status, ResolvedAt.Value));
    }

    public void MarkFailed(string reason)
    {
        Status = PaymentRequestStatus.Failed;
        FailureReason = reason;
        ResolvedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new PaymentRequestResolvedEvent(Id, MerchantId, Status, ResolvedAt.Value));
    }
}
