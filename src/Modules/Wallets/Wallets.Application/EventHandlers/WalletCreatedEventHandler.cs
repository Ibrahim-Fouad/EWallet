using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.BuildingBlocks.Common.Constants;
using EWallet.BuildingBlocks.Infrastructure.Contracts;
using EWallet.Modules.Wallets.Domain.Events;
using EWallet.Modules.Wallets.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EWallet.Modules.Wallets.Application.EventHandlers;

internal sealed class WalletCreatedEventHandler(
    IWalletRepository walletRepository,
    IEventBus eventBus,
    ILogger<WalletCreatedEventHandler> logger)
    : INotificationHandler<WalletCreatedEvent>
{
    public async Task Handle(WalletCreatedEvent notification, CancellationToken cancellationToken)
    {
        var systemWalletId = notification.Currency.ToString() == "EGP"
            ? SystemConstants.SystemWalletEgpId
            : SystemConstants.SystemWalletUsdId;

        var newWallet = await walletRepository.GetByIdAsync(notification.WalletId, cancellationToken);
        if (newWallet is null)
        {
            logger.LogWarning("New wallet {WalletId} not found when queuing welcome bonus.", notification.WalletId);
            return;
        }

        await eventBus.PublishAsync(new WelcomeBonusRequestedIntegrationEvent(
            SystemWalletId: systemWalletId,
            DestinationWalletId: newWallet.Id,
            DestinationPhoneNumber: newWallet.PhoneNumber,
            Amount: SystemConstants.WelcomeBonusAmount,
            Currency: notification.Currency.ToString()), cancellationToken);

        logger.LogInformation(
            "Welcome bonus queued for wallet {WalletId}",
            notification.WalletId);
    }
}
