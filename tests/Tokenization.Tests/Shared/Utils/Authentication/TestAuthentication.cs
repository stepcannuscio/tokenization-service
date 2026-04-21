using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Tokenization.Api.Authorization;

namespace Tokenization.Tests.Shared.Utils.Authentication;

// Test authentication scheme for integration tests
internal class TestAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public string DefaultUserId { get; set; } = string.Empty;
    public string DefaultTenantId { get; set; } = string.Empty;
    public string[] DefaultScopes { get; set; } = [Scopes.TokenCreate];
    public string[] DefaultRoles { get; set; } = [Roles.TokenAdmin];
}

internal class TestAuthorizationHandler(
    IOptionsMonitor<TestAuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<TestAuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If DefaultUserId is null, fail authentication
        if (string.IsNullOrEmpty(Options.DefaultUserId))
        {
            return Task.FromResult(AuthenticateResult.Fail("Authentication failed"));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Options.DefaultUserId),
            new(ClaimTypes.Name, Options.DefaultUserId),
            new("tenant_id", Options.DefaultTenantId)
        };

        // Add scope claims
        foreach (var scope in Options.DefaultScopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        // Add role claims
        foreach (var role in Options.DefaultRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}