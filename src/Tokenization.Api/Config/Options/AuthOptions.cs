namespace Tokenization.Api.Config.Options;

/// <summary>
/// Auth configuration options.
/// </summary>
internal sealed class AuthOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Auth";

    /// <summary>
    /// The OIDC issuer used for authorization and authentication.
    /// </summary>
    public string Authority { get; init; } = "https://dev-auth.example.com/";

    /// <summary>
    /// The audience that can access this API.
    /// </summary>
    public string Audience { get; init; } = "tokenization-api-dev";
}
