using System.Security.Claims;
using FluentAssertions;
using Tokenization.Api.Authorization;
using Xunit;

namespace Tokenization.Tests.Unit.Api.Authorization;

/// <summary>
/// Unit tests for the AuthenticationHandler class to ensure proper authorization logic.
/// </summary>
public class AuthorizationHandlerTests
{
    [Fact]
    public void IsAuthorized_WithValidScope_ShouldReturnTrue()
    {
        // Arrange
        var user = CreateUserWithScopes("tokens.create", "other.scope");
        const string requiredScope = "tokens.create";

        // Act
        var result = AuthorizationHandler.IsAuthorized(user, requiredScope);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthorized_WithInvalidScope_ShouldReturnFalse()
    {
        // Arrange
        var user = CreateUserWithScopes("tokens.create");
        const string requiredScope = "charges.submit";

        // Act
        var result = AuthorizationHandler.IsAuthorized(user, requiredScope);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthorized_WithValidRole_ShouldReturnTrue()
    {
        // Arrange
        var user = CreateUserWithRoles("token-admin");
        const string requiredScope = "tokens.create";
        var allowedRoles = new[] { "token-admin" };

        // Act
        var result = AuthorizationHandler.IsAuthorized(user, requiredScope, allowedRoles);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthorized_WithInvalidRole_ShouldReturnFalse()
    {
        // Arrange
        var user = CreateUserWithRoles("user");
        const string requiredScope = "tokens.create";
        var allowedRoles = new[] { "token-admin" };

        // Act
        var result = AuthorizationHandler.IsAuthorized(user, requiredScope, allowedRoles);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthorized_WithMultipleScopes_ShouldReturnTrueIfAnyMatch()
    {
        // Arrange
        var user = CreateUserWithScopes("tokens.create", "charges.submit");
        const string requiredScope = "tokens.create";

        // Act
        var result = AuthorizationHandler.IsAuthorized(user, requiredScope);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthorized_WithMultipleRoles_ShouldReturnTrueIfAnyMatch()
    {
        // Arrange
        var user = CreateUserWithRoles("user", "token-admin");
        const string requiredScope = "tokens.create";
        var allowedRoles = new[] { "token-admin", "tenant-service" };

        // Act
        var result = AuthorizationHandler.IsAuthorized(user, requiredScope, allowedRoles);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthorized_WithScopeAndRole_ShouldReturnTrueIfEitherMatches()
    {
        // Arrange
        var user = CreateUserWithScopes("tokens.create");
        const string requiredScope = "tokens.create";
        var allowedRoles = new[] { "token-admin" };

        // Act
        var result = AuthorizationHandler.IsAuthorized(user, requiredScope, allowedRoles);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthorized_WithNoScopesOrRoles_ShouldReturnFalse()
    {
        // Arrange
        var user = CreateUserWithScopes();
        const string requiredScope = "tokens.create";
        var allowedRoles = new[] { "token-admin" };

        // Act
        var result = AuthorizationHandler.IsAuthorized(user, requiredScope, allowedRoles);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthorized_WithSpaceSeparatedScopes_ShouldHandleCorrectly()
    {
        // Arrange
        var user = CreateUserWithSpaceSeparatedScopes("tokens.create charges.submit");
        const string requiredScope = "tokens.create";

        // Act
        var result = AuthorizationHandler.IsAuthorized(user, requiredScope);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthorized_WithCaseSensitiveScope_ShouldBeCaseSensitive()
    {
        // Arrange
        var user = CreateUserWithScopes("tokens.create");
        const string requiredScope = "TOKENS.CREATE";

        // Act
        var result = AuthorizationHandler.IsAuthorized(user, requiredScope);

        // Assert
        result.Should().BeFalse();
    }

    private static ClaimsPrincipal CreateUserWithScopes(params string[] scopes)
    {
        var claims = scopes.Select(scope => new Claim("scope", scope)).ToList();
        claims.Add(new Claim(ClaimTypes.Name, "test-user"));

        var identity = new ClaimsIdentity(claims, "test");
        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal CreateUserWithRoles(params string[] roles)
    {
        var claims = roles.Select(role => new Claim(ClaimTypes.Role, role)).ToList();
        claims.Add(new Claim(ClaimTypes.Name, "test-user"));

        var identity = new ClaimsIdentity(claims, "test");
        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal CreateUserWithSpaceSeparatedScopes(string scopes)
    {
        var claims = new List<Claim>
        {
            new("scope", scopes),
            new(ClaimTypes.Name, "test-user")
        };

        var identity = new ClaimsIdentity(claims, "test");
        return new ClaimsPrincipal(identity);
    }
}
