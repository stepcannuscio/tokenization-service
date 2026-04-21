using Tokenization.Api.Security.Config.Options;

namespace Tokenization.Api.Security.Config;

/// <summary>
/// DI registration for security headers.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Configures security headers middleware.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder,
        SecurityHeadersOptions? options = null)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}