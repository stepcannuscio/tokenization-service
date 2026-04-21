using Tokenization.Domain.Entities;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Domain.Abstractions;

/// <summary>
/// PCI-scoped persistence interface for <see cref="TokenRecord"/> entries.
/// The repository:
/// <list type="bullet">
///   <item><description>Accepts <see cref="EncryptedPayload"/> envelopes (no plaintext PCI data).</description></item>
///   <item><description>Executes tenant/customer lookups via blind-indexed shadow columns (no plaintext in predicates).</description></item>
///   <item><description>Exposes non-sensitive summaries; encrypted payload retrieval is opt-in and never decrypted here.</description></item>
/// </list>
/// </summary>
internal interface ITokenRecordRepository
{
    /// <summary>
    /// Creates and persists a new <see cref="TokenRecord"/> using a pre-encrypted PCI envelope.
    /// </summary>
    /// <param name="args">
    /// Non-sensitive attributes and token metadata (e.g., masked value, last4, tenant/customer IDs).
    /// If <c>args.Token</c> is <c>null</c>, the implementation may generate a token identifier.
    /// </param>
    /// <param name="encryptedPayload">
    /// Application-level encryption envelope (<see cref="EncryptedPayload"/>). The repository must not
    /// attempt to decrypt or log this value.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="TokenSummary"/> with display-safe fields.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> or <paramref name="encryptedPayload"/> is <c>null</c>.</exception>
    Task<TokenSummary> CreateAsync(CreateTokenArgs args, EncryptedPayload encryptedPayload, CancellationToken ct = default);

    /// <summary>
    /// Returns a non-sensitive summary of a token (no decryption).
    /// </summary>
    /// <param name="token">Public token identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="TokenSummary"/> with display-safe fields, or <c>null</c> if not found.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="token"/> is <c>null</c> or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when more than one entry exists for the given token.</exception>
    Task<TokenSummary?> GetSummaryByTokenAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Finds recent tokens for a tenant/customer pair using blind indexes (no plaintext in indexes).
    /// </summary>
    /// <param name="tenantId">Plaintext tenant identifier; hashed internally for querying.</param>
    /// <param name="customerId">Plaintext customer identifier; hashed internally for querying.</param>
    /// <param name="take">Maximum number of records to return (minimum 1).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="TokenSummary"/> items ordered by newest first. May be empty.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="tenantId"/> or <paramref name="customerId"/> is <c>null</c> or whitespace,
    /// or when <paramref name="take"/> is less than 1.
    /// </exception>
    Task<IReadOnlyList<TokenSummary>> FindByTenantCustomerAsync(
        string tenantId,
        string customerId,
        int take = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves the encrypted PCI envelope for a token (never decrypted by the repository).
    /// Intended for PCI-scoped callers that will decrypt using the domain encryption port.
    /// </summary>
    /// <param name="token">Public token identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The <see cref="EncryptedPayload"/> envelope if present, otherwise <c>null</c>.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="token"/> is <c>null</c> or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when more than one entry exists for the given token.</exception>
    Task<EncryptedPayload?> GetEncryptedPayloadAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Increments the usage count and updates the last-used timestamp.
    /// Implementations may deactivate the token when maximum uses or expiry are reached.
    /// </summary>
    /// <param name="token">Public token identifier to update.</param>
    /// <param name="nowUtc">Current UTC time used for last-used and expiry checks.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="TokenUsageResult"/> reflecting the updated usage count and activity state after the increment.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="token"/> is <c>null</c> or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the token does not exist.</exception>
    Task<TokenUsageResult> IncrementUsageAsync(string token, DateTimeOffset nowUtc, CancellationToken ct = default);

    /// <summary>
    /// Deactivates a token idempotently.
    /// </summary>
    /// <param name="token">Public token identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the token was deactivated or was already inactive; <c>false</c> if the token does not exist.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="token"/> is <c>null</c> or whitespace.</exception>
    Task<bool> DeactivateAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Deletes a token idempotently.
    /// </summary>
    /// <param name="token">Public token identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the token was deleted successfully; <c>false</c> if the token does not exist.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="token"/> is <c>null</c> or whitespace.</exception>
    Task<bool> DeleteAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Bulk creates multiple tokens efficiently.
    /// </summary>
    /// <param name="tokenData">Collection of token creation data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of created token summaries.</returns>
    Task<IReadOnlyList<TokenSummary>> BulkCreateAsync(
        IEnumerable<(CreateTokenArgs args, EncryptedPayload payload)> tokenData, CancellationToken ct = default);

    /// <summary>
    /// Bulk deactivates multiple tokens efficiently.
    /// </summary>
    /// <param name="tokens">Collection of tokens to deactivate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Number of tokens deactivated.</returns>
    Task<int> BulkDeactivateAsync(IEnumerable<string> tokens, CancellationToken ct = default);
}
