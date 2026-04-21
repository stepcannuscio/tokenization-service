namespace Tokenization.Domain.Exceptions;

/// <summary>
/// Base exception for tenant-related domain errors.
/// </summary>
public abstract class TenantDomainException : InvalidOperationException
{
    protected TenantDomainException(string message) : base(message)
    {
    }

    protected TenantDomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when tenant access is denied due to authorization rules.
/// </summary>
public sealed class TenantAccessDeniedException : TenantDomainException
{
    public string TenantId { get; }

    public TenantAccessDeniedException(string tenantId)
        : base($"Access denied for tenant: {tenantId}")
    {
        TenantId = tenantId;
    }

    public TenantAccessDeniedException(string tenantId, string message)
        : base(message)
    {
        TenantId = tenantId;
    }

    public TenantAccessDeniedException(string tenantId, string message, Exception innerException)
        : base(message, innerException)
    {
        TenantId = tenantId;
    }
}

/// <summary>
/// Exception thrown when tenant context is not found or unavailable.
/// </summary>
public sealed class TenantContextNotFoundException : TenantDomainException
{
    public TenantContextNotFoundException(string message) : base(message)
    {
    }

    public TenantContextNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
