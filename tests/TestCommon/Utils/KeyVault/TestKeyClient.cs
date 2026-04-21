using Azure;
using Azure.Security.KeyVault.Keys;
using Moq;

namespace Tokenization.Tests.Shared.Utils.KeyVault;

internal static class TestKeyClient
{
    public static Mock<KeyClient> ValidMock()
    {
        var mockClient = new Mock<KeyClient>();
        var keys = new List<KeyVaultKey>
        {
            TestKeyVaultKey.New("https://vault/keys/pay-kek/v1", "v1", DateTimeOffset.UtcNow)
        };
        var keyProps = keys.Select(k => k.Properties).ToList().AsReadOnly();
        var pagedKeys = Page<KeyProperties>.FromValues(keyProps, continuationToken: null, response: null!);
        var asyncPagedResponse = AsyncPageable<KeyProperties>.FromPages(new List<Page<KeyProperties>> { pagedKeys });

        mockClient.Setup(kp => kp.GetPropertiesOfKeysAsync(It.IsAny<CancellationToken>()))
            .Returns(asyncPagedResponse);
        
        return mockClient;
    }
}