using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Merchants.Domain.Errors;
using EWallet.Modules.Merchants.Domain.Repositories;

namespace EWallet.Modules.Merchants.Application.Queries.GetPaymentRequestById;

internal sealed class GetPaymentRequestByIdQueryHandler(
    IPaymentRequestRepository paymentRequestRepository,
    IWalletLookupService walletLookupService)
    : IQueryHandler<GetPaymentRequestByIdQuery, PaymentRequestDto>
{
    public async Task<Result<PaymentRequestDto>> Handle(
        GetPaymentRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        var paymentRequest = await paymentRequestRepository.GetByIdAsync(
            request.PaymentRequestId, cancellationToken);

        if (paymentRequest is null)
            return Result.Failure<PaymentRequestDto>(MerchantErrors.PaymentRequestNotFound);

        if (!request.IsAdmin && !request.IsMerchant)
        {
            var customerWalletResult = await walletLookupService.GetByIdAsync(
                paymentRequest.CustomerWalletId, cancellationToken);

            if (customerWalletResult.IsFailure ||
                customerWalletResult.Value.OwnerId != request.RequestingUserId)
                return Result.Failure<PaymentRequestDto>(MerchantErrors.Unauthorized);
        }

        if (request.IsMerchant && request.MerchantId.HasValue &&
            paymentRequest.MerchantId != request.MerchantId.Value)
            return Result.Failure<PaymentRequestDto>(MerchantErrors.Unauthorized);

        return Result.Success(new PaymentRequestDto(
            paymentRequest.Id,
            paymentRequest.MerchantId,
            paymentRequest.CustomerPhoneNumber,
            paymentRequest.Amount,
            paymentRequest.Currency,
            paymentRequest.Status.ToString(),
            paymentRequest.ExpiresAt,
            paymentRequest.ResolvedAt,
            paymentRequest.FailureReason,
            paymentRequest.CreatedAt));
    }
}
