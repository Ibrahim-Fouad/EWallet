namespace EWallet.BuildingBlocks.Infrastructure.Contracts;

public sealed record UserRegisteredIntegrationEvent(Guid UserId, string PhoneNumber);
