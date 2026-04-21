using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using Tokenization.Domain.Abstractions;
using Tokenization.Domain.Exceptions;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Application.Services;

/// <summary>
/// Default domain service for token lifecycle orchestration with comprehensive multi-tenant security.
/// Includes detailed logging for security monitoring and audit trails.
/// </summary>
internal sealed class TokenService : ITokenService
{
    private readonly ITokenRecordRepository _repo;
    private readonly IEncryptionService _crypto;
    private readonly ITenantContextService _tenantContext;
    private readonly ILogger<TokenService> _logger;

    /// <summary>Creates a new token service with the required domain ports.</summary>
    public TokenService(
        ITokenRecordRepository repository, 
        IEncryptionService crypto, 
        ITenantContextService tenantContext,
        ILogger<TokenService> logger)
    {
        _repo = repository ?? throw new ArgumentNullException(nameof(repository));
        _crypto = crypto ?? throw new ArgumentNullException(nameof(crypto));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<TokenSummary> IssueTokenAsync(CreateTokenArgs args, ReadOnlyMemory<byte> sensitivePayloadUtf8,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (sensitivePayloadUtf8.IsEmpty)
            throw new ArgumentException("Sensitive payload is required.", nameof(sensitivePayloadUtf8));

        _logger.LogInformation("Creating token for tenant {TenantId}, customer {CustomerId}, payment method {PaymentMethodType}",
            args.TenantId, args.CustomerId, args.PaymentMethodType);
        
        try
        {
            _tenantContext.ValidateTenantAccess(args.TenantId);
        }
        catch (TenantAccessDeniedException ex)
        {
            _logger.LogError("Token creation denied: {Message}. Requested tenant: {TenantId}", ex.Message, args.TenantId);
            throw;
        }
        
        byte[]? local = null;
        try
        {
            local = sensitivePayloadUtf8.ToArray();
            var plaintext = Encoding.UTF8.GetString(local);
            var envelope = await _crypto.EncryptAsync(plaintext, ct);
            
            var result = await _repo.CreateAsync(args, envelope, ct);
            _logger.LogInformation("Token created successfully: {Token} for tenant {TenantId}", result.Token, args.TenantId);
            return result;
        }
        finally
        {
            if (local is not null) CryptographicOperations.ZeroMemory(local);
        }
    }

    /// <inheritdoc />
    public async Task<TokenSummary> GetSummaryAsync(string token, CancellationToken ct = default)
    {
        _logger.LogDebug("Retrieving token summary for token: {Token}", token);
        
        var summary = await _repo.GetSummaryByTokenAsync(token, ct);
        if (summary is null)
        {
            _logger.LogWarning("Token not found: {Token}", token);
            throw new TokenNotFoundException(token);
        }
        
        try
        {
            _tenantContext.ValidateTenantAccess(summary.TenantId);
        }
        catch (TenantAccessDeniedException ex)
        {
            _logger.LogError("Token access denied: {Message}. Token: {Token}, Token tenant: {TokenTenantId}", 
                ex.Message, token, summary.TenantId);
            throw;
        }
        
        _logger.LogDebug("Token summary retrieved successfully: {Token} for tenant {TenantId}", token, summary.TenantId);
        return summary;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TokenSummary>> FindByTenantCustomerAsync(string tenantId, string customerId,
        int take = 50, CancellationToken ct = default)
    {
        _logger.LogInformation("Finding tokens for tenant {TenantId}, customer {CustomerId}, take: {Take}",
            tenantId, customerId, take);

        try
        {
            _tenantContext.ValidateTenantAccess(tenantId);
        }
        catch (TenantAccessDeniedException ex)
        {
            _logger.LogError("Token query denied: {Message}. Requested tenant: {TenantId}", ex.Message, tenantId);
            throw;
        }

        var result = await _repo.FindByTenantCustomerAsync(tenantId, customerId, Math.Max(1, take), ct);
        _logger.LogInformation("Found {Count} tokens for tenant {TenantId}, customer {CustomerId}",
            result.Count, tenantId, customerId);

        return result;
    }

    /// <inheritdoc />
    public async Task<TokenSummary> RedeemTokenAsync(string token, DateTimeOffset nowUtc, CancellationToken ct = default)
    {
        _logger.LogInformation("Redeeming token: {Token} at {Timestamp}", token, nowUtc);

        var summary = await _repo.GetSummaryByTokenAsync(token, ct);
        if (summary is null)
        {
            _logger.LogWarning("Token not found for redemption: {Token}", token);
            throw new TokenNotFoundException(token);
        }

        try
        {
            _tenantContext.ValidateTenantAccess(summary.TenantId);
        }
        catch (TenantAccessDeniedException ex)
        {
            _logger.LogError("Token redemption denied: {Message}. Token: {Token}, Token tenant: {TokenTenantId}",
                ex.Message, token, summary.TenantId);
            throw;
        }

        if (!summary.IsActive)
        {
            _logger.LogWarning("Token redemption failed: token is inactive. Token: {Token}", token);
            throw new TokenInactiveException(token);
        }

        if (summary.IsExpired(nowUtc))
        {
            _logger.LogWarning(
                "Token redemption failed: token is expired. Token: {Token}, ExpiresAt: {ExpiresAt}, Now: {Now}",
                token, summary.ExpiresAt, nowUtc);
            throw new TokenExpiredException(token);
        }

        if (summary.IsUsageExceeded())
        {
            _logger.LogWarning(
                "Token usage limit exceeded, deactivating: {Token}, max uses: {MaxUses}, current usage: {UsageCount}",
                token, summary.MaxUses, summary.UsageCount);
            await _repo.DeactivateAsync(token, ct);
            throw new TokenUsageExceededException(token);
        }

        var result = await _repo.IncrementUsageAsync(token, nowUtc, ct);
        _logger.LogInformation("Token redeemed successfully: {Token}, usage count: {UsageCount}", token,
            result.UsageCount);

        if (result.IsUsageExceeded())
        {
            _logger.LogInformation(
                "Token usage limit reached, deactivating: {Token}, max uses: {MaxUses}, current usage: {UsageCount}",
                token, result.MaxUses, result.UsageCount);
            await _repo.DeactivateAsync(token, ct);
            result = result with { IsActive = false };
        }

        return summary with
        {
            LastUsedAt = result.LastUsedAt,
            IsActive = result.IsActive,
            UsageCount = result.UsageCount
        };
    }

    /// <inheritdoc />
    public async Task<DetokenizedToken> DetokenizeTokenAsync(string token, CancellationToken ct = default)
    {
        _logger.LogInformation("Detokenizing token: {Token}", token);
        
        var summary = await _repo.GetSummaryByTokenAsync(token, ct);
        if (summary is null)
        {
            _logger.LogWarning("Token not found to detokenize: {Token}", token);
            throw new TokenNotFoundException(token);
        }
        
        try
        {
            _tenantContext.ValidateTenantAccess(summary.TenantId);
        }
        catch (TenantAccessDeniedException ex)
        {
            _logger.LogError("Token detokenize denied: {Message}. Token: {Token}, Token tenant: {TokenTenantId}", 
                ex.Message, token, summary.TenantId);
            throw;
        }
        
        var envelope = await _repo.GetEncryptedPayloadAsync(token, ct);
        if (envelope is null)
        {
            _logger.LogWarning("Encrypted payload not found for token: {Token}", token);
            throw new TokenNotFoundException(token);
        }
        
        var result = await _crypto.DecryptAsync(envelope, ct);
        _logger.LogInformation("Token payload decrypted successfully: {Token}", token);
        return new DetokenizedToken(result, summary);
    }

    /// <inheritdoc />
    public async Task DeleteTokenAsync(string token, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting token: {Token}", token);
        
        var summary = await _repo.GetSummaryByTokenAsync(token, ct);
        if (summary is null)
        {
            _logger.LogWarning("Token not found for deletion: {Token}", token);
            throw new TokenNotFoundException(token);
        }
        
        try
        {
            _tenantContext.ValidateTenantAccess(summary.TenantId);
        }
        catch (TenantAccessDeniedException ex)
        {
            _logger.LogError("Token deletion denied: {Message}. Token: {Token}, Token tenant: {TokenTenantId}", 
                ex.Message, token, summary.TenantId);
            throw;
        }
        
        var ok = await _repo.DeleteAsync(token, ct);
        if (!ok)
        {
            _logger.LogWarning("Token deletion failed: {Token}", token);
            throw new TokenNotFoundException(token);
        }
        
        _logger.LogInformation("Token deletion successfully: {Token}", token);
    }
}
