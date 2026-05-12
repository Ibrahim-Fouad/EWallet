using EWallet.Modules.Merchants.Domain.Entities;
using EWallet.Modules.Merchants.Domain.Enums;
using EWallet.Modules.Merchants.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EWallet.Modules.Merchants.Infrastructure.Persistence.Repositories;

internal sealed class PaymentRequestRepository(MerchantsDbContext context) : IPaymentRequestRepository
{
    public void Add(PaymentRequest request) => context.PaymentRequests.Add(request);

    public async Task<PaymentRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.PaymentRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<PaymentRequest?> GetActivePendingForMerchantAndPhoneAsync(
        Guid merchantId,
        string customerPhoneNumber,
        CancellationToken cancellationToken = default) =>
        await context.PaymentRequests.FirstOrDefaultAsync(
            r => r.MerchantId == merchantId
                 && r.CustomerPhoneNumber == customerPhoneNumber
                 && r.Status == PaymentRequestStatus.Pending,
            cancellationToken);

    public async Task<IReadOnlyList<PaymentRequest>> GetPendingForCustomerAsync(
        string customerPhoneNumber,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await context.PaymentRequests
            .Where(r => r.CustomerPhoneNumber == customerPhoneNumber
                        && r.Status == PaymentRequestStatus.Pending
                        && r.ExpiresAt > now)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentRequest?> GetByTransferTransactionIdAsync(
        Guid transferTransactionId,
        CancellationToken cancellationToken = default) =>
        await context.PaymentRequests.FirstOrDefaultAsync(
            r => r.TransferTransactionId == transferTransactionId, cancellationToken);
}
