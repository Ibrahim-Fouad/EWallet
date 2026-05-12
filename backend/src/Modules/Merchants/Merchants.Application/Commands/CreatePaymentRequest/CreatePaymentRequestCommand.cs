using EWallet.BuildingBlocks.Application.Abstractions;

namespace EWallet.Modules.Merchants.Application.Commands.CreatePaymentRequest;

public sealed record CreatePaymentRequestCommand(
    Guid MerchantId,
    string CustomerPhoneNumber,
    decimal Amount) : ICommand<CreatePaymentRequestResponse>;

public sealed record CreatePaymentRequestResponse(Guid Id, string Status, DateTimeOffset ExpiresAt);
