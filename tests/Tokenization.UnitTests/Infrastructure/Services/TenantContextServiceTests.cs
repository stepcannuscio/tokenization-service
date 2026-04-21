using FluentAssertions;
using Tokenization.Domain.Exceptions;
using Tokenization.Infrastructure.Authorization;
using Tokenization.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Tokenization.Tests.Unit.Infrastructure.Services;

public sealed class TenantContextServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly Mock<ILogger<TenantContextService>> _logger = new();

    [Fact]
    public void GetCurrentTenantId_WithTenantClaim_ReturnsTenantId()
    {
        var service = CreateService(CreateHttpContext(authenticated: true, tenantClaimType: TenantClaims.TenantId, tenantId: "tenant-123"));

        service.GetCurrentTenantId().Should().Be("tenant-123");
    }

    [Fact]
    public void GetCurrentTenantId_WithLegacyMerchantClaim_ReturnsTenantId()
    {
        var service = CreateService(CreateHttpContext(authenticated: true, tenantClaimType: TenantClaims.MerchantIdAlias, tenantId: "tenant-legacy"));

        service.GetCurrentTenantId().Should().Be("tenant-legacy");
    }

    [Fact]
    public void GetCurrentTenantId_WithoutTenantContext_Throws()
    {
        var service = CreateService(CreateHttpContext(authenticated: true, tenantClaimType: null, tenantId: null));

        FluentActions.Invoking(service.GetCurrentTenantId)
            .Should().Throw<TenantContextNotFoundException>();
    }

    [Fact]
    public void ValidateTenantAccess_WithUnauthorizedTenant_Throws()
    {
        var service = CreateService(CreateHttpContext(authenticated: true, tenantClaimType: TenantClaims.TenantId, tenantId: "tenant-123"));

        FluentActions.Invoking(() => service.ValidateTenantAccess("tenant-456"))
            .Should().Throw<TenantAccessDeniedException>();
    }

    [Fact]
    public void ValidateTenantAccess_WithMatchingTenant_Succeeds()
    {
        var service = CreateService(CreateHttpContext(authenticated: true, tenantClaimType: TenantClaims.TenantId, tenantId: "tenant-123"));

        FluentActions.Invoking(() => service.ValidateTenantAccess("tenant-123"))
            .Should().NotThrow();
    }

    private TenantContextService CreateService(HttpContext httpContext)
    {
        _httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        return new TenantContextService(_httpContextAccessor.Object, _logger.Object);
    }

    private static HttpContext CreateHttpContext(bool authenticated, string? tenantClaimType, string? tenantId)
    {
        var httpContext = new DefaultHttpContext();

        if (!authenticated)
        {
            return httpContext;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-123"),
            new(ClaimTypes.Name, "user-123")
        };

        if (!string.IsNullOrEmpty(tenantClaimType) && tenantId is not null)
        {
            claims.Add(new Claim(tenantClaimType, tenantId));
        }

        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        return httpContext;
    }
}
