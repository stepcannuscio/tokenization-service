using System.Security.Claims;

namespace Tokenization.Api.Authorization;

/// <summary>
/// Provides utility methods for authentication and authorization checks in the tokenization API.
/// </summary>
internal static class AuthorizationHandler
{
    /// <summary>
    /// Determines whether a user is authorized to access a resource based on OAuth scopes and roles.
    /// </summary>
    /// <param name="user">The authenticated user's claims principal.</param>
    /// <param name="scope">The required OAuth scope for access.</param>
    /// <param name="roles">Optional roles that also grant access.</param>
    /// <returns>
    /// <c>true</c> if the user has the required scope or any of the specified roles; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method checks both OAuth scopes (from JWT tokens) and traditional role-based authorization.
    /// The user is authorized if they have either the required scope or any of the specified roles.
    /// </remarks>
    public static bool IsAuthorized(ClaimsPrincipal user, string scope, params string[] roles)
    {
        var scopes = user.FindAll("scope").Select(c => c.Value).SelectMany(v => v.Split(' '));
        return scopes.Contains(scope, StringComparer.Ordinal) || roles.Any(user.IsInRole);
    }
}