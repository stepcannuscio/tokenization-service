using Azure.Security.KeyVault.Keys;

namespace Tokenization.Tests.Shared.Utils.KeyVault;

public static class TestKeyVaultKey
{
    public static KeyVaultKey New(string id, string version, DateTimeOffset createdOn)
    {
        var jsonWebKey = new JsonWebKey(new List<KeyOperation>
        {
            new("wrapKey"),
            new("unwrapKey")
        });
        var props = Props(id, version, createdOn);
        return KeyModelFactory.KeyVaultKey(props, jsonWebKey);
    }

    private static KeyProperties Props(string id, string version, DateTimeOffset createdOn) =>
        KeyModelFactory.KeyProperties(
            id: new Uri(id),
            name: "pay-kek",
            version: version,
            createdOn: createdOn,
            updatedOn: createdOn,
            recoveryLevel: "Recoverable",
            recoverableDays: null,
            managed: false);
}