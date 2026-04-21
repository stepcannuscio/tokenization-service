using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Tokenization.Infrastructure.Authorization;

namespace Tokenization.Api.Authentication.Development;

internal sealed class DevelopmentAuthSchemeOptions : AuthenticationSchemeOptions
{
    public bool Enabled { get; set; }

    public string BearerToken { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public string[] Scopes { get; set; } = [];

    public string[] Roles { get; set; } = [];
}

internal sealed class DevelopmentAuthHandler(
    IOptionsMonitor<DevelopmentAuthSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<DevelopmentAuthSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DevelopmentBearer";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Options.Enabled)
        {
            return Task.FromResult(AuthenticateResult.Fail("Development auth is disabled."));
        }

        if (!Request.Headers.TryGetValue("Authorization", out var headerValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var authorizationHeader = headerValues.ToString();
        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var bearerToken = authorizationHeader["Bearer ".Length..].Trim();
        if (!string.Equals(bearerToken, Options.BearerToken, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("The supplied development bearer token is invalid."));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Options.UserId),
            new(ClaimTypes.Name, Options.UserId),
            new(TenantClaims.TenantId, Options.TenantId)
        };

        claims.AddRange(Options.Scopes.Select(scope => new Claim("scope", scope)));
        claims.AddRange(Options.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
