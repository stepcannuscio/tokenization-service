namespace Tokenization.Api.Config.Options;

/// <summary>
/// CORS configuration options.
/// </summary>
internal sealed class CorsOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Cors";
    
    /// <summary>
    /// The name of the CORS policy to use.
    /// </summary>
    public string PolicyName { get; init; } = "StrictCors";

    /// <summary>
    /// Allowed origins for CORS.
    /// </summary>
    public string[] AllowedOrigins { get; init; } =
        ["https://localhost:3000", "https://dev-tenant-portal.example.com"];

    /// <summary>
    /// Allowed HTTP methods for CORS.
    /// </summary>
    public string[] AllowedMethods { get; init; } = ["GET", "POST", "OPTIONS"];

    /// <summary>
    /// Whether to allow any headers.
    /// </summary>
    public bool AllowAnyHeader { get; init; } = true;

    /// <summary>
    /// Whether to disallow credentials.
    /// </summary>
    public bool DisallowCredentials { get; init; } = true;
}
