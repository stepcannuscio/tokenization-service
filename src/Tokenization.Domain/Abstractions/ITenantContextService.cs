using Tokenization.Domain.Exceptions;

namespace Tokenization.Domain.Abstractions;

/// <summary>
/// Service for accessing the current tenant context in multi-tenant scenarios.
/// </summary>
public interface ITenantContextService
{
    /// <summary>
    /// Gets the current tenant ID from the authenticated user's context.
    /// </summary>
    /// <returns>The current tenant ID.</returns>
    /// <exception cref="TenantContextNotFoundException">Thrown when no tenant context is available.</exception>
    string GetCurrentTenantId();

    /// <summary>
    /// Attempts to get the current tenant ID without throwing an exception.
    /// </summary>
    /// <returns>The current tenant ID if available; otherwise, <c>null</c>.</returns>
    string? TryGetCurrentTenantId();

    /// <summary>
    /// Validates that the current user has access to the specified tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID to validate access for.</param>
    /// <returns><c>true</c> if the user has access to the tenant; otherwise, <c>false</c>.</returns>
    bool IsAuthorizedForTenant(string tenantId);

    /// <summary>
    /// Validates tenant access and throws an exception when access is denied.
    /// </summary>
    /// <param name="tenantId">The tenant ID to validate access for.</param>
    /// <exception cref="TenantAccessDeniedException">Thrown when the user doesn't have access to the specified tenant.</exception>
    void ValidateTenantAccess(string tenantId);
}
