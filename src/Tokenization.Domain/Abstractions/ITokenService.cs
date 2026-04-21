using System.Security.Cryptography;
using Tokenization.Domain.Entities;
using Tokenization.Domain.Exceptions;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Domain.Abstractions;

/// <summary>
/// Orchestrates token lifecycle business rules (creation, redemption/use, deactivation)
/// while delegating persistence and encryption to domain ports.
/// </summary>
internal interface ITokenService
{
    /// <summary>
    /// Issues a new token by encrypting the provided PCI payload and persisting a <see cref="TokenRecord"/>.
    /// Business validations are applied before persistence.
    /// </summary>
    /// <param name="args">
    /// Non-sensitive attributes and metadata for the token (e.g., masked value, last4, tenant/customer IDs).
    /// </param>
    /// <param name="sensitivePayloadUtf8">
    /// UTF-8 encoded PCI payload (e.g., PAN JSON). This value is encrypted inside the service and never stored in plaintext.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="TokenSummary"/> with display-safe fields.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sensitivePayloadUtf8"/> is empty, or when required fields in
    /// <paramref name="args"/> (such as MaskedData or Last4) are missing/invalid.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when the encryption operation or database entry creation fails.</exception>
    /// <exception cref="TenantAccessDeniedException">Thrown when the caller does not have access to the specified tenant.</exception>
    /// <exception cref="CryptographicException">Thrown if the encryption fails.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the <c>IKeyProvider</c> fails to wrap the DEK.</exception>
    Task<TokenSummary> IssueTokenAsync(CreateTokenArgs args, ReadOnlyMemory<byte> sensitivePayloadUtf8, CancellationToken ct = default);

    /// <summary>
    /// Returns a non-sensitive summary of a token.
    /// </summary>
    /// <param name="token">Public token identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="TokenSummary"/> with display-safe fields.</returns>
    /// <exception cref="TokenNotFoundException">Thrown when the specified <paramref name="token"/> does not exist.</exception>
    /// <exception cref="TenantAccessDeniedException">Thrown when the caller does not have access to the specified <paramref name="token"/>.</exception>
    Task<TokenSummary> GetSummaryAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Finds recent tokens for a tenant/customer pair using blind-indexed lookups (no plaintext in predicates).
    /// </summary>
    /// <param name="tenantId">Plaintext tenant identifier; hashed internally for querying.</param>
    /// <param name="customerId">Plaintext customer identifier; hashed internally for querying.</param>
    /// <param name="take">Maximum number of records to return (minimum 1).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of <see cref="TokenSummary"/> items ordered by newest first. May be empty.</returns>
    /// <exception cref="TenantAccessDeniedException">Thrown when the caller does not have access to the specified <paramref name="tenantId"/>.</exception>
    Task<IReadOnlyList<TokenSummary>> FindByTenantCustomerAsync(string tenantId, string customerId, int take = 50, CancellationToken ct = default);

    /// <summary>
    /// Validates business rules (active, not expired, usage limits) and records a token use.
    /// </summary>
    /// <param name="token">Public token identifier to redeem.</param>
    /// <param name="nowUtc">Current UTC time used for expiry checks.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="TokenSummary"/> reflecting the updated usage count and activity state after redemption.
    /// </returns>
    /// <exception cref="TokenNotFoundException">Thrown if the specified <paramref name="token"/> does not exist.</exception>
    /// <exception cref="TokenInactiveException">Thrown if the token is inactive and cannot be used.</exception>
    /// <exception cref="TokenExpiredException">Thrown if the token is expired relative to <paramref name="nowUtc"/>.</exception>
    /// <exception cref="TokenUsageExceededException">Thrown if the token exceeds its allowed number of uses during redemption.</exception>
    /// <exception cref="TenantAccessDeniedException">Thrown when the caller does not have access to the specified <paramref name="token"/>.</exception>
    Task<TokenSummary> RedeemTokenAsync(string token, DateTimeOffset nowUtc, CancellationToken ct = default);

    /// <summary>
    /// Decrypts the secure payload for a token. This method should be invoked only within PCI-scoped code paths.
    /// </summary>
    /// <param name="token">Public token identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The decrypted UTF-8 plaintext payload.</returns>
    /// <exception cref="TokenNotFoundException">Thrown when the specified <paramref name="token"/> does not exist or has no encrypted payload.</exception>
    /// <exception cref="TenantAccessDeniedException">Thrown when the caller does not have access to the specified <paramref name="token"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the <c>IEncryptionService</c> fails to decrypt the token.</exception>
    Task<DetokenizedToken> DetokenizeTokenAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Deletes a token idempotently. Subsequent calls have no effect.
    /// </summary>
    /// <param name="token">Public token identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the token is deleted.</returns>
    /// <exception cref="TokenNotFoundException">Thrown when the specified <paramref name="token"/> does not exist.</exception>
    /// <exception cref="TenantAccessDeniedException">Thrown when the caller does not have access to the specified <paramref name="token"/>.</exception>
    Task DeleteTokenAsync(string token, CancellationToken ct = default);
}
