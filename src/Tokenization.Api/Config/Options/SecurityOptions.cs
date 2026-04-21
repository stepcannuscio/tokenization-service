namespace Tokenization.Api.Config.Options;

/// <summary>
/// Security configuration options.
/// </summary>
internal sealed class SecurityOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Security";
    
    /// <summary>
    /// The HTTPS port for redirection.
    /// </summary>
    public int HttpsPort { get; init; } = 443;

    /// <summary>
    /// The HSTS max age in days.
    /// </summary>
    public int HstsMaxAgeDays { get; init; } = 180;

    /// <summary>
    /// The JWT clock skew in minutes.
    /// </summary>
    public int JwtClockSkewMinutes { get; init; } = 2;
}