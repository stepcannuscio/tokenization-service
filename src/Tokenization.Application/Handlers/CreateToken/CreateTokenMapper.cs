using System.Globalization;
using Tokenization.Domain.Enums;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Application.Handlers.CreateToken;

/// <summary>
/// Default mapper for create token feature.
/// Performs masking server-side and prepares domain args with plaintext payload.
/// </summary>
internal static class CreateTokenMapper
{
    /// <summary>Maps a <see cref="CreateTokenCommand"/> into domain <see cref="CreateTokenArgs"/>.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cmd"/> is <c>null</c>.</exception>
    public static CreateTokenArgs ToCreateTokenArgs(this CreateTokenCommand cmd)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        // Normalize & create masked display like "************1234"
        var pan = cmd.Card?.Pan.Trim() ?? string.Empty;
        var last4 = pan.Length >= 4 ? pan[^4..] : pan;
        var masked = pan.Length <= 4 ? last4 : new string('*', pan.Length - 4) + last4;

        var tokenType = GetEnumValue<TokenType>(cmd.TokenType);
        if (tokenType is TokenType.OneTime) cmd.MaxUses = 1;

        return new CreateTokenArgs(
            Token: null,
            MaskedData: masked,
            Last4: last4,
            PaymentMethodType: GetEnumValue<PaymentMethodType>(cmd.PaymentMethodType),
            Network: cmd.Network,
            PaymentMethodMetadata: null,
            Currency: cmd.Currency,
            Country: cmd.Country,
            TenantId: cmd.TenantId,
            CustomerId: cmd.CustomerId,
            TokenType: tokenType,
            MaxUses: cmd.MaxUses,
            InitialTransactionId: cmd.InitialTransactionId,
            StoredCredentialInitiator: TryGetEnumValue<StoredCredentialInitiator>(cmd.StoredCredentialInitiator),
            StoredCredentialReason: TryGetEnumValue<StoredCredentialReason>(cmd.StoredCredentialReason),
            ExpiresAt: cmd.ExpiresAt);
    }

    /// <summary>Maps a <see cref="CreateTokenCommand"/> into a minimal plaintext blob for the domain to encrypt.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cmd"/> is <c>null</c>.</exception>
    public static ReadOnlyMemory<byte> ToSensitivePayload(this CreateTokenCommand cmd)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        // Simple normalized pipe format
        var normalized = string.Join("|", "card", cmd.Card?.Pan,
            cmd.Card?.ExpMonth.ToString("00", CultureInfo.InvariantCulture),
            cmd.Card?.ExpYear.ToString("0000", CultureInfo.InvariantCulture),
            cmd.Card?.CardholderName ?? string.Empty);

        return System.Text.Encoding.UTF8.GetBytes(normalized);
    }

    private static TEnum? TryGetEnumValue<TEnum>(string? value) where TEnum : struct
    {
        if (value is null) return null;
        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var result)
            ? result
            : null;
    }

    private static TEnum GetEnumValue<TEnum>(string value) where TEnum : struct
    {
        return Enum.Parse<TEnum>(value, ignoreCase: true);
    }
}
