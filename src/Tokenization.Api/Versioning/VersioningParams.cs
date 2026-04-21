namespace Tokenization.Api.Versioning;

/// <summary>
/// Defines the headers used for versioning requests and responses.
/// </summary>
internal static class VersioningParams
{
    /// <summary>
    /// The header used by the client to optionally set a specific version.
    /// </summary>
    public const string Header = "X-API-Version";
    
    /// <summary>
    /// The query param used by the client to optionally set a specific version.
    /// </summary>
    public const string Query = "api-version";
}