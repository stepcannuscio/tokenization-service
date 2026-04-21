using Tokenization.Domain.Abstractions;
using Tokenization.Domain.ValueObjects;

namespace Tokenization.Tests.Shared.Utils.Clients;

internal sealed class TestKeyClient : IKeyClient<string>
{
    public string Client { get; set; } = "fake";

    public KeyVersionInfo VersionInfo { get; set; } = new()
    {
        KekKeyId = "kid1"
    };

    public override string ToString() =>
        $"{VersionInfo.KekKeyId} (current={VersionInfo.IsCurrent}, created={VersionInfo.CreatedAt:O})";

    public static TestKeyClient Valid(string keyId, DateTimeOffset created, bool isCurrent = false) =>
        new()
        {
            VersionInfo = new KeyVersionInfo
            {
                KekKeyId = keyId,
                CreatedAt = created,
                IsCurrent = isCurrent
            }
        };

    public static string CurrentCacheKey(string keyName)
    {
        return $"{AllCacheKey(keyName)}/current";
    }

    public static string AllCacheKey(string keyName)
    {
        return $"KeyClientCache/{keyName}";
    }
}
