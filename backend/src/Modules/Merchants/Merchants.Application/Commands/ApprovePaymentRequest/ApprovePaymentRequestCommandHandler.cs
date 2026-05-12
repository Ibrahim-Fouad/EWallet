using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Merchants.Application.Abstractions;
using EWallet.Modules.Merchants.Domain.Enums;
using EWallet.Modules.Merchants.Domain.Errors;
using EWallet.Modules.Merchants.Domain.Repositories;
using EWallet.Modules.Transactions.Application.Commands.Transfer;
using MediatR;

namespace EWallet.Modules.Merchants.Application.Commands.ApprovePaymentRequest;

internal sealed class ApprovePaymentRequestCommandHandler(
    IPaymentRequestRepository paymentRequestRepository,
    IWalletLookupService walletLookupService,
    IMerchantUnitOfWork unitOfWork,
    IMediator mediator)
    : ICommandHandler<ApprovePaymentRequestCommand, ApprovePaymentRequestResponse>
{
    public async Task<Result<ApprovePaymentRequestResponse>> Handle(
        ApprovePaymentRequestCommand request,
        CancellationToken cancellationToken)
    {
        var paymentRequest = await paymentRequestRepository.GetByIdAsync(
            request.PaymentRequestId, cancellationToken);

        if (paymentRequest is null)
            return Result.Failure<ApprovePaymentRequestResponse>(MerchantErrors.PaymentRequestNotFound);

        if (paymentRequest.Status != PaymentRequestStatus.Pending)
            return Result.Failure<ApprovePaymentRequestResponse>(MerchantErrors.RequestNotPending);

        if (paymentRequest.ExpiresAt <= DateTimeOffset.UtcNow)
            return Result.Failure<ApprovePaymentRequestResponse>(MerchantErrors.RequestExpired);

        var customerWalletResult = await walletLookupService.GetByIdAsync(
            paymentRequest.CustomerWalletId, cancellationToken);
        if (customerWalletResult.IsFailure)
            return Result.Failure<ApprovePaymentRequestResponse>(
                Error.NotFound("Merchant.CustomerWalletNotFound", "Customer wallet not found."));

        var customerWallet = customerWalletResult.Value;

        if (customerWallet.OwnerId != request.RequestingUserId)
            return Result.Failure<ApprovePaymentRequestResponse>(MerchantErrors.Unauthorized);

        if (customerWallet.Balance < paymentRequest.Amount)
            return Result.Failure<ApprovePaymentRequestResponse>(MerchantErrors.InsufficientBalance);

        var merchantWalletResult = await walletLookupService.GetByIdAsync(
            paymentRequest.MerchantWalletId, cancellationToken);
        if (merchantWalletResult.IsFailure)
            return Result.Failure<ApprovePaymentRequestResponse>(
                Error.NotFound("Merchant.MerchantWalletNotFound", "Merchant wallet not found."));

        var transferCommand = new TransferCommand(
            IdempotencyKey: paymentRequest.Id.ToString(),
            SourcePhoneNumber: customerWallet.PhoneNumber,
            DestinationPhoneNumber: merchantWalletResult.Value.PhoneNumber,
            Amount: paymentRequest.Amount,
            RequestingUserId: request.RequestingUserId,
            Notes: $"Merchant payment {paymentRequest.Id}");

        var transferResult = await mediator.Send(transferCommand, cancellationToken);
        if (transferResult.IsFailure)
            return Result.Failure<ApprovePaymentRequestResponse>(transferResult.Error);

        paymentRequest.MarkApproved(transferResult.Value.TransactionId);

        await unitOfWork.DispatchDomainEventsAsync(cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new ApprovePaymentRequestResponse(
            transferResult.Value.TransactionId,
            paymentRequest.Status.ToString()));
    }
}
