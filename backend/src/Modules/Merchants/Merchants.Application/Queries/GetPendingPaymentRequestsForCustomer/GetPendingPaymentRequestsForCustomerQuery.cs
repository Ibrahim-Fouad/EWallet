using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.Modules.Merchants.Application.Queries.GetPaymentRequestById;

namespace EWallet.Modules.Merchants.Application.Queries.GetPendingPaymentRequestsForCustomer;

public sealed record GetPendingPaymentRequestsForCustomerQuery(
    string CustomerPhoneNumber) : IQuery<IReadOnlyList<PaymentRequestDto>>;
