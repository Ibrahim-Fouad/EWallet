using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.Modules.Merchants.Application.Queries.GetPaymentRequestById;
using EWallet.Modules.Merchants.Domain.Repositories;

namespace EWallet.Modules.Merchants.Application.Queries.GetPendingPaymentRequestsForCustomer;

internal sealed class GetPendingPaymentRequestsForCustomerQueryHandler(
    IPaymentRequestRepository paymentRequestRepository)
    : IQueryHandler<GetPendingPaymentRequestsForCustomerQuery, IReadOnlyList<PaymentRequestDto>>
{
    public async Task<Result<IReadOnlyList<PaymentRequestDto>>> Handle(
        GetPendingPaymentRequestsForCustomerQuery request,
        CancellationToken cancellationToken)
    {
        var requests = await paymentRequestRepository.GetPendingForCustomerAsync(
            request.CustomerPhoneNumber, cancellationToken);

        var dtos = requests
            .Select(r => new PaymentRequestDto(
                r.Id,
                r.MerchantId,
                r.CustomerPhoneNumber,
                r.Amount,
                r.Currency,
                r.Status.ToString(),
                r.ExpiresAt,
                r.ResolvedAt,
                r.FailureReason,
                r.CreatedAt))
            .ToList();

        return Result.Success<IReadOnlyList<PaymentRequestDto>>(dtos);
    }
}
