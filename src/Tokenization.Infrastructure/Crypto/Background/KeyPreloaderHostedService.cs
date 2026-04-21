using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tokenization.Domain.Abstractions;

namespace Tokenization.Infrastructure.Crypto.Background;

/// <summary>
/// Background hosted service that proactively preloads (and retries) key material on app startup.
/// This primes the <see cref="IKeyProvider"/> and downstream caches to avoid first-request latency.
/// </summary>
/// <remarks>
/// The service performs a bounded retry loop and logs failures per attempt.
/// </remarks>
internal sealed class KeyPreloaderHostedService(
    ILogger<KeyPreloaderHostedService> logger,
    List<string> keyNames,
    IKeyProvider keyProvider)
    : IHostedService
{
    /// <summary>
    /// Invoked by the host to start the service; attempts to preload active key versions.
    /// </summary>
    /// <param name="ct">A token to observe while waiting for the task to complete.</param>
    public async Task StartAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;

        foreach (var keyName in keyNames)
        {
            await PreloadKeysWithRetry(keyName, ct);
        }
    }

    private async Task PreloadKeysWithRetry(string keyName, CancellationToken ct)
    {
        var retryCount = 5;
        while (true)
        {
            try
            {
                await keyProvider.PreloadKeysAsync(keyName, ct);
                logger.LogInformation("Preloaded keys for '{KeyName}'.", keyName);
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Executing retry to preload keys for '{KeyName}'.", keyName);
                retryCount--;
            }

            if (retryCount > 0) continue;
            logger.LogError("Failed to preload keys for '{KeyName}'.", keyName);
            break;
        }
    }

    /// <summary>
    /// Invoked by the host to stop the service. No additional shutdown work is required.
    /// </summary>
    /// <param name="ct">A token to observe while waiting for the task to complete.</param>
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}