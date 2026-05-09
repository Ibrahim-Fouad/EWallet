using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace EWallet.API.Extensions;

internal static class RateLimiterExtensions
{
    /// <summary>
    /// Registers the sliding-window rate limiter policies used by the API.
    /// </summary>
    internal static IServiceCollection AddApiRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // 10 transfers per minute per client; 6 segments → 10-second granularity.
            options.AddSlidingWindowLimiter("transfer", limiterOptions =>
            {
                limiterOptions.PermitLimit          = 10;
                limiterOptions.Window               = TimeSpan.FromMinutes(1);
                limiterOptions.SegmentsPerWindow    = 6;
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit           = 0;
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }
}
