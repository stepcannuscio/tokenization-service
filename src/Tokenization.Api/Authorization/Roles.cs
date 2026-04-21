namespace Tokenization.Api.Authorization;

/// <summary>
/// Defines user roles used for role-based authorization in the tokenization API.
/// These roles provide additional authorization mechanisms beyond OAuth scopes.
/// </summary>
internal static class Roles
{
    /// <summary>
    /// Administrative role with full access to tokenization operations and system management.
    /// </summary>
    public const string TokenAdmin = "token-admin";
}
