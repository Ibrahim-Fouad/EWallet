namespace EWallet.BuildingBlocks.Infrastructure.Contracts;

public sealed record WelcomeBonusRequestedIntegrationEvent(
    Guid SystemWalletId,
    Guid DestinationWalletId,
    string DestinationPhoneNumber,
    decimal Amount,
    string Currency);
