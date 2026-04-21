namespace Tokenization.Api.Config.Options;

/// <summary>
/// Swagger configuration options.
/// </summary>
internal sealed class SwaggerOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Swagger";

    /// <summary>
    /// Indicates if Swagger is enabled.
    /// </summary>
    public bool Enabled { get; init; }
}