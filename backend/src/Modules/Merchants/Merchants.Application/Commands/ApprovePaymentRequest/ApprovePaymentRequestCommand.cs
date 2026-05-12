using EWallet.BuildingBlocks.Application.Abstractions;

namespace EWallet.Modules.Merchants.Application.Commands.ApprovePaymentRequest;

public sealed record ApprovePaymentRequestCommand(
    Guid PaymentRequestId,
    Guid RequestingUserId) : ICommand<ApprovePaymentRequestResponse>;

public sealed record ApprovePaymentRequestResponse(Guid TransactionId, string Status);
