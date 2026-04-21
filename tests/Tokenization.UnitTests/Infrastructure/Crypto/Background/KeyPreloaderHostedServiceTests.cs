using Moq;
using Tokenization.Domain.Abstractions;
using Tokenization.Infrastructure.Crypto.Background;
using Xunit;

namespace Tokenization.Tests.Unit.Infrastructure.Crypto.Background;

public class KeyPreloaderHostedServiceTests
{
    private const int RetryCount = 5;
    private readonly List<string> _keys = ["pay-kek", "blind-index-kek"];

    private static Mock<ILogger<KeyPreloaderHostedService>> MockLogger()
    {
        return new Mock<ILogger<KeyPreloaderHostedService>>();
    }

    [Fact]
    public async Task StartAsync_WhenCancelled_Completes_Without_Work()
    {
        var logger = MockLogger();
        var keyProvider = new Mock<IKeyProvider>(MockBehavior.Strict);
        var service = new KeyPreloaderHostedService(logger.Object, _keys, keyProvider.Object);

        var cancelled = new CancellationToken(canceled: true);
        
        await service.StartAsync(cancelled);
        
        keyProvider.Verify(p => p.PreloadKeysAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_Succeeds_First_Try_Logs_Info_Once()
    {
        var logger = MockLogger();
        var keyProvider = new Mock<IKeyProvider>(MockBehavior.Strict);
        keyProvider.Setup(p => p.PreloadKeysAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        
        var service = new KeyPreloaderHostedService(logger.Object, _keys, keyProvider.Object);

        await service.StartAsync(CancellationToken.None);

        keyProvider.Verify(p => p.PreloadKeysAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(_keys.Count));

        logger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(_keys.Count));
        
        logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task StartAsync_Retries_On_Failures_Then_Succeeds_And_Logs_Accordingly()
    {
        var logger = MockLogger();
        var keyProvider = new Mock<IKeyProvider>(MockBehavior.Strict);

        foreach (var key in _keys)
        {
            keyProvider
                .SetupSequence(p => p.PreloadKeysAsync(key, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("boom-1"))
                .ThrowsAsync(new Exception("boom-2"))
                .ThrowsAsync(new Exception("boom-3"))
                .Returns(Task.CompletedTask); // success on 4th attempt
        }

        var service = new KeyPreloaderHostedService(logger.Object, _keys, keyProvider.Object);

        await service.StartAsync(CancellationToken.None);

        keyProvider.Verify(p => p.PreloadKeysAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(_keys.Count * (RetryCount - 1)));

        // 3 error logs per key with exceptions 
        logger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.Is<Exception>(ex => true),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(_keys.Count * (RetryCount - 2)));

        // final success info log exactly once per key
        logger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(_keys.Count));
    }

    [Fact]
    public async Task StartAsync_All_5_Attempts_Fail_Logs_5_Retries_Then_Final_Failure()
    {
        var logger = MockLogger();
        var keyProvider = new Mock<IKeyProvider>(MockBehavior.Strict);
        keyProvider
            .Setup(p => p.PreloadKeysAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("always-fail"));

        var service = new KeyPreloaderHostedService(logger.Object, _keys, keyProvider.Object);

        await service.StartAsync(CancellationToken.None);

        // With retryCount = 5, the loop attempts at most 5 times, for each key, then logs a final failure and exits.
        keyProvider.Verify(p => p.PreloadKeysAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(_keys.Count * RetryCount));

        // 5 retry error logs (with exceptions) and 1 final error log for each key
        logger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.Is<Exception>(ex => true),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(_keys.Count * (RetryCount + 1)));
    }

    [Fact]
    public async Task StopAsync_Completes_No_Op()
    {
        var service = new KeyPreloaderHostedService(MockLogger().Object, _keys, new Mock<IKeyProvider>().Object);
        await service.StopAsync(CancellationToken.None); // nothing to assert—no throws is success
    }
}