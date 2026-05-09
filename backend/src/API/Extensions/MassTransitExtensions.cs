using EWallet.API.Infrastructure;
using EWallet.BuildingBlocks.Application.Abstractions;
using EWallet.Modules.Notifications.Infrastructure.Consumers;
using EWallet.Modules.Transactions.Infrastructure.Consumers;
using EWallet.Modules.Transactions.Infrastructure.Persistence;
using EWallet.Modules.Transactions.Infrastructure.Sagas;
using EWallet.Modules.Wallets.Infrastructure.Consumers;
using MassTransit;

namespace EWallet.API.Extensions;

internal static class MassTransitExtensions
{
    /// <summary>
    /// Registers MassTransit with the EF Core outbox, the transfer saga, all consumers,
    /// and the RabbitMQ transport.
    /// </summary>
    internal static IServiceCollection AddMassTransitWithRabbitMq(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped(typeof(CorrelationIdConsumeFilter<>));
        services.AddScoped(typeof(CorrelationIdPublishFilter<>));
        services.AddScoped(typeof(CorrelationIdSendFilter<>));

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            // ── Saga ────────────────────────────────────────────────────────────
            // ExistingDbContext<TransactionsDbContext>() reuses the scoped instance so
            // that saga-state changes, Transaction-entity updates (via activities), and
            // outbox records are all committed in ONE SaveChangesAsync call — atomically.
            x.AddSagaStateMachine<TransferSagaStateMachine, TransferSagaState>()
                .EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                    r.ExistingDbContext<TransactionsDbContext>();
                });

            // ── Consumers ───────────────────────────────────────────────────────
            x.AddConsumer<DebitSourceWalletConsumer>();
            x.AddConsumer<CreditDestinationWalletConsumer>();
            x.AddConsumer<ReverseDebitConsumer>();
            x.AddConsumer<WelcomeBonusConsumer>();
            x.AddConsumer<TransferCompletedConsumer>();
            x.AddConsumer<UserRegisteredConsumer>();

            // ── EF Core outbox (non-consumer / HTTP-handler publish) ────────────
            // Registers the outbox delivery hosted service that polls
            // transactions.outbox_message and forwards staged messages to RabbitMQ.
            // IPublishEndpoint in HTTP-handler scope uses this to stage messages
            // in-memory; SaveChangesAsync() commits them atomically with entity changes.
            x.AddEntityFrameworkOutbox<TransactionsDbContext>(o =>
            {
                o.UseSqlServer();
                o.DuplicateDetectionWindow = TimeSpan.FromHours(25);
            });

            // Apply the EF outbox middleware to EVERY receive endpoint automatically.
            // UseEntityFrameworkOutbox is a per-endpoint call; AddConfigureEndpointsCallback
            // is the correct hook to apply it globally across all endpoints.
            // MUST be registered BEFORE UsingRabbitMq / ConfigureEndpoints.
            x.AddConfigureEndpointsCallback((ctx, _, cfg) =>
            {
                cfg.UseConsumeFilter(typeof(CorrelationIdConsumeFilter<>), ctx);
                cfg.UseEntityFrameworkOutbox<TransactionsDbContext>(ctx);
            });

            // ── RabbitMQ transport ─────────────────────────────────────────────
            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration.GetConnectionString("rabbitmq"));

                cfg.UsePublishFilter(typeof(CorrelationIdPublishFilter<>), ctx);
                cfg.UseSendFilter(typeof(CorrelationIdSendFilter<>), ctx);

                // Global retry: 1 s → 5 s → 10 s before the message moves to the error queue.
                cfg.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10)));

                cfg.ConfigureEndpoints(ctx);
            });
        });

        // Make IEventBus available globally for publishing cross-module integration events
        // (e.g., from the registration Razor Page). Publishes directly via IPublishEndpoint —
        // no outbox staging, which is acceptable for non-financial registration events.
        services.AddScoped<IEventBus, ApiMassTransitEventBus>();

        return services;
    }
}

/// <summary>
/// File-scoped adapter so the API layer can publish via IEventBus without taking a hard
/// dependency on any module's internal MassTransitEventBus implementation.
/// </summary>
file sealed class ApiMassTransitEventBus(IPublishEndpoint publishEndpoint) : IEventBus
{
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class => publishEndpoint.Publish(message, cancellationToken);
}

