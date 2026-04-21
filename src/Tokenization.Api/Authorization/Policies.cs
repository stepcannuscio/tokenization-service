namespace Tokenization.Api.Authorization;

/// <summary>
/// Defines authorization policies used for access control in the tokenization API.
/// These policies combine scopes and roles to create comprehensive authorization rules.
/// </summary>
internal static class Policies
{
    /// <summary>
    /// Policy that allows users to read token information.
    /// Requires either the <see cref="Scopes.TokenRead"/> scope or the <see cref="Roles.TokenAdmin"/> role.
    /// </summary>
    public const string CanReadTokens = nameof(CanReadTokens);

    /// <summary>
    /// Policy that allows users to create new tokens.
    /// Requires either the <see cref="Scopes.TokenCreate"/> scope or the <see cref="Roles.TokenAdmin"/> role.
    /// </summary>
    public const string CanCreateTokens = nameof(CanCreateTokens);

    /// <summary>
    /// Policy that allows users to delete tokens.
    /// Requires either the <see cref="Scopes.TokenDelete"/> scope or the <see cref="Roles.TokenAdmin"/> role.
    /// </summary>
    public const string CanDeleteTokens = nameof(CanDeleteTokens);

    /// <summary>
    /// Policy that allows users to detokenize tokens.
    /// Requires either the <see cref="Scopes.TokenDetokenize"/> scope or the <see cref="Roles.TokenAdmin"/> role.
    /// </summary>
    public const string CanDetokenizeTokens = nameof(CanDetokenizeTokens);
}
