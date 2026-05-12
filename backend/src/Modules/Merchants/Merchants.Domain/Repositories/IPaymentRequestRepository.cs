using EWallet.Modules.Merchants.Domain.Entities;

namespace EWallet.Modules.Merchants.Domain.Repositories;

public interface IPaymentRequestRepository
{
    void Add(PaymentRequest request);
    Task<PaymentRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentRequest?> GetActivePendingForMerchantAndPhoneAsync(Guid merchantId, string customerPhoneNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentRequest>> GetPendingForCustomerAsync(string customerPhoneNumber, CancellationToken cancellationToken = default);
    Task<PaymentRequest?> GetByTransferTransactionIdAsync(Guid transferTransactionId, CancellationToken cancellationToken = default);
}
