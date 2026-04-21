namespace Tokenization.Api.Authorization;

/// <summary>
/// Defines OAuth scopes used for authorization in the tokenization API.
/// These scopes control access to specific operations and resources.
/// </summary>
internal static class Scopes
{
    /// <summary>
    /// Scope required to read token information.
    /// </summary>
    public const string TokenRead = "tokens.read";

    /// <summary>
    /// Scope required to create new tokens.
    /// </summary>
    public const string TokenCreate = "tokens.create";
    
    /// <summary>
    /// Scope required to delete tokens.
    /// </summary>
    public const string TokenDelete = "tokens.delete";
        
    /// <summary>
    /// Scope required to detokenize tokens.
    /// </summary>
    public const string TokenDetokenize = "tokens.detokenize";
}