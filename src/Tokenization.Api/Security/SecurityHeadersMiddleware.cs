namespace Tokenization.Api.Security;

/// <summary>
/// Middleware that adds comprehensive security headers for PCI compliance and security best practices.
/// </summary>
internal sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        AddSecurityHeaders(context.Response);
        await next(context);
    }

    private static void AddSecurityHeaders(HttpResponse response)
    {
        // Content Security Policy — 'unsafe-inline' is required for Swagger UI script and style injection
        response.Headers.ContentSecurityPolicy = "default-src 'self'; " +
                                                 "script-src 'self' 'unsafe-inline'; " +
                                                 "style-src 'self' 'unsafe-inline'; " +
                                                 "img-src 'self' data: https:; " +
                                                 "font-src 'self' data:; " +
                                                 "connect-src 'self'; " +
                                                 "frame-ancestors 'none';";

        // Prevent MIME type sniffing
        response.Headers.XContentTypeOptions = "nosniff";

        // X-XSS-Protection intentionally omitted — deprecated and superseded by the CSP above

        // Prevent clickjacking
        response.Headers.XFrameOptions = "DENY";

        // Referrer policy
        response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Permissions policy
        response.Headers["Permissions-Policy"] = "camera=(), " +
                                                 "microphone=(), " +
                                                 "geolocation=(), " +
                                                 "interest-cohort=()";

        // PCI compliance headers
        response.Headers["X-PCI-Compliant"] = "true";
        response.Headers["X-Security-Level"] = "PCI-DSS";

        // Cache control
        response.Headers.CacheControl = "no-store, no-cache, must-revalidate, private";
        response.Headers.Pragma = "no-cache";
        response.Headers.Expires = "0";
    }
}
