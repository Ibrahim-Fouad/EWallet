using EWallet.BuildingBlocks.Application.Abstractions;

namespace EWallet.Modules.Merchants.Application.Commands.RejectPaymentRequest;

public sealed record RejectPaymentRequestCommand(
    Guid PaymentRequestId,
    Guid RequestingUserId) : ICommand;
