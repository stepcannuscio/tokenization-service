using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tokenization.Domain.Abstractions;
using Tokenization.Domain.Exceptions;
using Tokenization.Infrastructure.Authorization;

namespace Tokenization.Infrastructure.Services;

/// <summary>
/// Default implementation of the tenant context service that extracts tenant information from claims.
/// </summary>
public sealed class TenantContextService(
    IHttpContextAccessor httpContextAccessor,
    ILogger<TenantContextService> logger)
    : ITenantContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor =
        httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    private readonly ILogger<TenantContextService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public string GetCurrentTenantId()
    {
        var tenantId = TryGetCurrentTenantId();
        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning(
                "No tenant context available. User may not be authenticated or tenant claim is missing. UserId: {UserId}, Claims: {Claims}",
                GetUserId(),
                GetAvailableClaims());
            throw new TenantContextNotFoundException(
                "No tenant context is available. User may not be authenticated or tenant claim is missing.");
        }

        return tenantId;
    }

    public string? TryGetCurrentTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var tenantId = httpContext.User.FindFirst(TenantClaims.TenantId)?.Value;
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            return tenantId;
        }

        tenantId = httpContext.User.FindFirst(TenantClaims.MerchantIdAlias)?.Value;
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            return tenantId;
        }

        return null;
    }

    public bool IsAuthorizedForTenant(string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            return false;
        }

        var currentTenantId = TryGetCurrentTenantId();
        return !string.IsNullOrEmpty(currentTenantId) &&
               string.Equals(currentTenantId, tenantId, StringComparison.OrdinalIgnoreCase);
    }

    public void ValidateTenantAccess(string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));
        }

        if (!IsAuthorizedForTenant(tenantId))
        {
            throw new TenantAccessDeniedException(tenantId);
        }
    }

    private string GetUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
               httpContext?.User?.FindFirst("sub")?.Value ??
               httpContext?.User?.Identity?.Name ??
               "unknown";
    }

    private string GetAvailableClaims()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return "none";
        }

        return string.Join(", ", httpContext.User.Claims.Select(c => c.Type));
    }
}
