using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common;
using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Merchants.Application.Abstractions;
using EWallet.Modules.Merchants.Application.Jobs;
using EWallet.Modules.Merchants.Domain.Entities;
using EWallet.Modules.Merchants.Domain.Enums;
using EWallet.Modules.Merchants.Domain.Errors;
using EWallet.Modules.Merchants.Domain.Repositories;
using EWallet.Modules.Notifications.Application.Abstractions;
using Hangfire;

namespace EWallet.Modules.Merchants.Application.Commands.CreatePaymentRequest;

internal sealed class CreatePaymentRequestCommandHandler(
    IMerchantRepository merchantRepository,
    IPaymentRequestRepository paymentRequestRepository,
    IWalletLookupService walletLookupService,
    INotificationService notificationService,
    IMerchantUnitOfWork unitOfWork,
    IBackgroundJobClient backgroundJobClient)
    : ICommandHandler<CreatePaymentRequestCommand, CreatePaymentRequestResponse>
{
    public async Task<Result<CreatePaymentRequestResponse>> Handle(
        CreatePaymentRequestCommand request,
        CancellationToken cancellationToken)
    {
        var merchant = await merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
        if (merchant is null)
            return Result.Failure<CreatePaymentRequestResponse>(MerchantErrors.NotFound);

        if (merchant.Status != MerchantStatus.Active)
            return Result.Failure<CreatePaymentRequestResponse>(MerchantErrors.NotActive);

        var merchantWalletResult = await walletLookupService.GetByIdAsync(merchant.ReceivingWalletId, cancellationToken);
        if (merchantWalletResult.IsFailure || !merchantWalletResult.Value.IsActive)
            return Result.Failure<CreatePaymentRequestResponse>(
                Error.Validation("Merchant.WalletInactive", "Merchant receiving wallet is not active."));

        var merchantWallet = merchantWalletResult.Value;

        if (merchantWallet.PhoneNumber == request.CustomerPhoneNumber)
            return Result.Failure<CreatePaymentRequestResponse>(MerchantErrors.SelfPaymentForbidden);

        var customerWalletResult = await walletLookupService.GetByPhoneNumberAsync(
            request.CustomerPhoneNumber, cancellationToken);
        if (customerWalletResult.IsFailure)
            return Result.Failure<CreatePaymentRequestResponse>(
                Error.NotFound("Merchant.CustomerNotFound", "Customer wallet not found for the given phone number."));

        var customerWallet = customerWalletResult.Value;

        if (customerWallet.Currency != merchant.Currency)
            return Result.Failure<CreatePaymentRequestResponse>(MerchantErrors.CurrencyMismatch);

        var existing = await paymentRequestRepository.GetActivePendingForMerchantAndPhoneAsync(
            merchant.Id, request.CustomerPhoneNumber, cancellationToken);
        if (existing is not null)
            return Result.Failure<CreatePaymentRequestResponse>(MerchantErrors.DuplicatePending);

        var paymentRequest = PaymentRequest.Create(
            merchant.Id,
            merchant.ReceivingWalletId,
            request.CustomerPhoneNumber,
            customerWallet.Id,
            request.Amount,
            merchant.Currency);

        paymentRequestRepository.Add(paymentRequest);

        await unitOfWork.DispatchDomainEventsAsync(cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        backgroundJobClient.Schedule<IExpirePaymentRequestJob>(
            j => j.RunAsync(paymentRequest.Id),
            TimeSpan.FromMinutes(2));

        await notificationService.SendPaymentRequestCreatedAsync(
            customerWallet.OwnerId,
            paymentRequest.Id,
            merchant.BusinessName,
            paymentRequest.Amount,
            paymentRequest.Currency,
            paymentRequest.ExpiresAt,
            cancellationToken);

        return Result.Success(new CreatePaymentRequestResponse(
            paymentRequest.Id,
            paymentRequest.Status.ToString(),
            paymentRequest.ExpiresAt));
    }
}
