using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Tokenization.Tests.Shared.Utils.Authentication;

/// <summary>
/// Utility class for generating test JWT tokens with various claims for integration testing.
/// </summary>
public static class TestJwtTokenGenerator
{
    private const string TestSecret = "ThisIsAVeryLongSecretKeyForTestingPurposesOnlyAndShouldNotBeUsedInProduction";
    private const string TestIssuer = "https://test-issuer.com";
    private const string TestAudience = "tokenization-api";

    /// <summary>
    /// Generates a test JWT token with the specified tenant ID and scopes.
    /// </summary>
    /// <param name="tenantId">The tenant ID to include in the token.</param>
    /// <param name="scopes">The scopes to include in the token.</param>
    /// <param name="roles">Optional roles to include in the token.</param>
    /// <returns>A JWT token string.</returns>
    public static string GenerateToken(string? tenantId, string scopes, params string[] roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-123"),
            new(ClaimTypes.Name, "Test User"),
            new("scope", scopes)
        };

        if (!string.IsNullOrEmpty(tenantId))
        {
            claims.Add(new Claim("tenant_id", tenantId));
        }

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a test JWT token with an organization ID claim instead of a tenant ID claim.
    /// </summary>
    /// <param name="organizationId">The organization ID to include in the token.</param>
    /// <param name="scopes">The scopes to include in the token.</param>
    /// <returns>A JWT token string.</returns>
    public static string GenerateTokenWithOrgId(string organizationId, string scopes)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-123"),
            new(ClaimTypes.Name, "Test User"),
            new("scope", scopes),
            new("org_id", organizationId)
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a test JWT token with an explicit tenant ID claim.
    /// </summary>
    /// <param name="tenantId">The tenant ID to include in the token.</param>
    /// <param name="scopes">The scopes to include in the token.</param>
    /// <returns>A JWT token string.</returns>
    public static string GenerateTokenWithTenantId(string tenantId, string scopes)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-123"),
            new(ClaimTypes.Name, "Test User"),
            new("scope", scopes),
            new("tenant_id", tenantId)
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates an expired test JWT token for testing token expiration scenarios.
    /// </summary>
    /// <param name="tenantId">The tenant ID to include in the token.</param>
    /// <param name="scopes">The scopes to include in the token.</param>
    /// <returns>An expired JWT token string.</returns>
    public static string GenerateExpiredToken(string tenantId, string scopes)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-123"),
            new(ClaimTypes.Name, "Test User"),
            new("scope", scopes),
            new("tenant_id", tenantId)
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(-1), // Expired 1 hour ago
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a test JWT token with invalid audience for testing audience validation.
    /// </summary>
    /// <param name="tenantId">The tenant ID to include in the token.</param>
    /// <param name="scopes">The scopes to include in the token.</param>
    /// <returns>A JWT token string with invalid audience.</returns>
    public static string GenerateTokenWithInvalidAudience(string tenantId, string scopes)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-123"),
            new(ClaimTypes.Name, "Test User"),
            new("scope", scopes),
            new("tenant_id", tenantId)
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: "invalid-audience", // Invalid audience
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
