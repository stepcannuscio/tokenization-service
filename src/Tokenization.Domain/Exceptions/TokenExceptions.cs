namespace Tokenization.Domain.Exceptions;

/// <summary>
/// Base type for token business rule violations.
/// </summary>
internal abstract class TokenRuleViolationException(string message) : InvalidOperationException(message);

/// <summary>
/// Thrown when a token is not found.
/// </summary>
internal sealed class TokenNotFoundException(string token)
    : TokenRuleViolationException($"Token '{token}' was not found.");

/// <summary>
/// Thrown when a token is inactive and may not be used.
/// </summary>
internal sealed class TokenInactiveException(string token)
    : TokenRuleViolationException($"Token '{token}' is inactive.");

/// <summary>
/// Thrown when a token is expired.
/// </summary>
internal sealed class TokenExpiredException(string token) : TokenRuleViolationException($"Token '{token}' is expired.");

/// <summary>
/// Thrown when a token has exceeded its allowed number of uses.
/// </summary>
internal sealed class TokenUsageExceededException(string token)
    : TokenRuleViolationException($"Token '{token}' has exceeded its allowed number of uses.");
