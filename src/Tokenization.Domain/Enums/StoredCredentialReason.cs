namespace Tokenization.Domain.Enums;

/// <summary>
/// Describes the business reason for using a stored credential, commonly required for MIT classifications.
/// </summary>
internal enum StoredCredentialReason
{
    /// <summary>Recurring series under an agreement (subscription, membership, etc.).</summary>
    Recurring,

    /// <summary>Installment plan where payments are split over time for a single purchase.</summary>
    Installment,

    /// <summary>Unscheduled/variable amount (e.g., usage-based or on-demand charges).</summary>
    Unscheduled,

    /// <summary>Charge added after the initial purchase (e.g., minibar, damages) where permitted.</summary>
    DelayedCharge,

    /// <summary>Authorized no-show fee (e.g., lodging, car rental) per network/industry rules.</summary>
    NoShow
}