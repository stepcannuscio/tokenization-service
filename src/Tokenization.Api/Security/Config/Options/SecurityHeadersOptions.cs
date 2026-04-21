namespace Tokenization.Api.Security.Config.Options;

/// <summary>
/// Options for configuring security headers.
/// </summary>
internal sealed class SecurityHeadersOptions
{
    /// <summary>
    /// Controls which resources the user agent can load and execute (to mitigate XSS attacks).
    /// </summary>
    public string ContentSecurityPolicy { get; set; } = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none';";

    /// <summary>
    /// Controls how much referrer information is sent.
    /// </summary>
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";
    
    /// <summary>
    /// Controls the use of browser APIs.
    /// </summary>
    public string PermissionsPolicy { get; set; } = 
        "camera=(), " +
        "microphone=(), " +
        "geolocation=(), " +
        "interest-cohort=()";
}