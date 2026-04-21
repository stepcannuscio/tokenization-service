using Moq;
using Tokenization.Domain.Abstractions;
using Tokenization.Tests.Shared.Utils.ValueObjects;

namespace Tokenization.Tests.Shared.Utils.Crypto;

internal static class TestKeyProvider
{
    public static Mock<IKeyProvider> ValidMock()
    {
        var mockKeyProvider = new Mock<IKeyProvider>();
        var testSignKey = new byte[] { 1, 2, 3, 4, 5 };
        var dataToWrap = new byte[] { 1, 2, 3, 4, 5 };
        var envelope = TestEncryptedPayload.Valid();

        mockKeyProvider.Setup(kp => kp.SignDataAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(testSignKey);

        mockKeyProvider
            .Setup(c => c.WrapKeyAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelope.WrapPayload);

        mockKeyProvider.Setup(kp => kp.UnwrapKeyAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataToWrap);

        return mockKeyProvider;
    }
}
