namespace Tokenization.Domain.Enums;

/// <summary>
/// Identifies who initiated the use of a stored credential, aligned with network
/// stored-credential frameworks (e.g., Customer-Initiated vs. Merchant-Initiated).
/// </summary>
internal enum StoredCredentialInitiator
{
    /// <summary>The cardholder/customer initiated the transaction (CIT).</summary>
    Customer,

    /// <summary>The merchant initiated the transaction using a credential on file (MIT).</summary>
    Merchant
}