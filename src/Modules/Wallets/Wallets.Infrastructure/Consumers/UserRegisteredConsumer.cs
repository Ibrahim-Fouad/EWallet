using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Wallets.Application.Commands.CreateWallet;
using EWallet.Modules.Wallets.Domain.Enums;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EWallet.Modules.Wallets.Infrastructure.Consumers;

public sealed class UserRegisteredConsumer(
    ISender sender,
    ILogger<UserRegisteredConsumer> logger) : IConsumer<UserRegisteredIntegrationEvent>
{
    public async Task Consume(ConsumeContext<UserRegisteredIntegrationEvent> context)
    {
        var msg = context.Message;

        var result = await sender.Send(
            new CreateWalletCommand(msg.UserId, msg.PhoneNumber, Currency.EGP),
            context.CancellationToken);

        if (result.IsFailure)
            logger.LogWarning(
                "Auto wallet creation failed for user {UserId}: {Error}",
                msg.UserId, result.Error);
        else
            logger.LogInformation("Auto EGP wallet created for user {UserId}", msg.UserId);
    }
}
