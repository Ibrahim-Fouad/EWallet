using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Merchants.Application.Abstractions;
using EWallet.Modules.Merchants.Domain.Enums;
using EWallet.Modules.Merchants.Domain.Errors;
using EWallet.Modules.Merchants.Domain.Repositories;

namespace EWallet.Modules.Merchants.Application.Commands.RejectPaymentRequest;

internal sealed class RejectPaymentRequestCommandHandler(
    IPaymentRequestRepository paymentRequestRepository,
    IWalletLookupService walletLookupService,
    IMerchantUnitOfWork unitOfWork)
    : ICommandHandler<RejectPaymentRequestCommand>
{
    public async Task<Result> Handle(
        RejectPaymentRequestCommand request,
        CancellationToken cancellationToken)
    {
        var paymentRequest = await paymentRequestRepository.GetByIdAsync(
            request.PaymentRequestId, cancellationToken);

        if (paymentRequest is null)
            return Result.Failure(MerchantErrors.PaymentRequestNotFound);

        if (paymentRequest.Status != PaymentRequestStatus.Pending)
            return Result.Failure(MerchantErrors.RequestNotPending);

        if (paymentRequest.ExpiresAt <= DateTimeOffset.UtcNow)
            return Result.Failure(MerchantErrors.RequestExpired);

        var customerWalletResult = await walletLookupService.GetByIdAsync(
            paymentRequest.CustomerWalletId, cancellationToken);
        if (customerWalletResult.IsFailure)
            return Result.Failure(Error.NotFound("Merchant.CustomerWalletNotFound", "Customer wallet not found."));

        if (customerWalletResult.Value.OwnerId != request.RequestingUserId)
            return Result.Failure(MerchantErrors.Unauthorized);

        paymentRequest.MarkRejected();

        await unitOfWork.DispatchDomainEventsAsync(cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
