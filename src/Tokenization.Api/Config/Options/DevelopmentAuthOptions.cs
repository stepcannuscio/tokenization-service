namespace Tokenization.Api.Config.Options;

internal sealed class DevelopmentAuthOptions
{
    public const string SectionName = "DevelopmentAuth";

    public bool Enabled { get; init; }

    public string BearerToken { get; init; } = "local-dev-token";

    public string UserId { get; init; } = "local-dev-user";

    public string TenantId { get; init; } = "demo-tenant";

    public string[] Scopes { get; init; } =
    [
        Authorization.Scopes.TokenRead,
        Authorization.Scopes.TokenCreate,
        Authorization.Scopes.TokenDelete,
        Authorization.Scopes.TokenDetokenize
    ];

    public string[] Roles { get; init; } = [Authorization.Roles.TokenAdmin];
}
