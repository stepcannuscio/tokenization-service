namespace Tokenization.Domain.ValueObjects;

/// <summary>
/// Plaintext card values (PCI data). Keep process-local and never persist CVC.
/// </summary>
internal sealed class CardPlaintext
{
    /// <summary>Primary account number (PAN), numeric only.</summary>
    public string Pan { get; set; } = null!;

    /// <summary>Two-digit/one- or two-digit month (1..12).</summary>
    public int ExpMonth { get; set; }

    /// <summary>Four-digit year.</summary>
    public int ExpYear { get; set; }

    /// <summary>Cardholder name (optional).</summary>
    public string? CardholderName { get; set; }
}
