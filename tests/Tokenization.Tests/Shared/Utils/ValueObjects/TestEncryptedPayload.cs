using Tokenization.Domain.ValueObjects;

namespace Tokenization.Tests.Shared.Utils.ValueObjects;

internal static class TestEncryptedPayload
{
    public static EncryptedPayload Valid(
        string kid = "kek://inmem/v1",
        string alg = "AES-CBC-DEV") =>
        new()
        {
            Ciphertext = "cipher"u8.ToArray(),
            Nonce = new byte[12],
            Tag = new byte[16],
            WrapPayload = new KeyWrapPayload
            {
                WrappedDek = new byte[48],
                KekKeyId = kid,
                Algorithm = alg,
                WrappedAt = DateTimeOffset.UtcNow
            }
        };
}