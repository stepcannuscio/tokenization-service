namespace Tokenization.Domain.Enums;

/// <summary>
/// Indicates how a token is intended to be used with respect to lifecycle.
/// </summary>
internal enum TokenType
{
    /// <summary>Single-use token intended for one authorization only.</summary>
    OneTime,

    /// <summary>Token intended to be stored for reuse.</summary>
    StoredCredential
}