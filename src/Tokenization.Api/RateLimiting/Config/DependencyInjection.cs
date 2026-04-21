using System.Threading.RateLimiting;

namespace Tokenization.Api.RateLimiting.Config;

/// <summary>
/// DI registration for rate limiting.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Configures optimal rate limiting using built-in ASP.NET Core rate limiting.
    /// This provides enterprise-grade rate limiting with IP-based limiting, tiered access,
    /// and automatic violation handling.
    /// </summary>
    /// <param name="services">The service collection</param>
    public static void AddTokenizationRateLimiting(this IServiceCollection services)
    {
        // Configure built-in rate limiting with tiered policies and IP-based limiting
        services.AddRateLimiter(options =>
        {
            // Anonymous users - strict IP-based limits
            options.AddPolicy("AnonymousPolicy", context =>
            {
                var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetTokenBucketLimiter(remoteIp, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 10,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 5,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                    TokensPerPeriod = 10,
                    AutoReplenishment = true
                });
            });

            // Authenticated users - user-based limits (falls back to IP if no user)
            options.AddPolicy("AuthenticatedPolicy", context =>
            {
                var key = context.User.Identity?.IsAuthenticated == true
                    ? context.User.Identity.Name ?? "unknown"
                    : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 100,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 20,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                    TokensPerPeriod = 100,
                    AutoReplenishment = true
                });
            });

            // Token creation - IP-based limits with stricter enforcement
            options.AddPolicy("TokenCreationPolicy", context =>
            {
                var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(remoteIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 5
                });
            });

            // Token detokenization - IP-based limits with the strictest enforcement
            options.AddPolicy("TokenDetokenizationPolicy", context =>
            {
                var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(remoteIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 2
                });
            });

            // Global rejection response with security monitoring
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                var remoteIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();

                // Log rate limit violations for security monitoring
                logger?.LogWarning("Rate limit exceeded for IP {RemoteIp} on {Method} {Path}",
                    remoteIp, context.HttpContext.Request.Method, context.HttpContext.Request.Path);

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", token);
            };
        });
    }
}