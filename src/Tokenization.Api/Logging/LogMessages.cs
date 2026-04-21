namespace Tokenization.Api.Logging;

internal partial class LogMessages
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "Created token for tenant {TenantId} with tokenId {TokenId}")]
    public static partial void TokenCreated(ILogger logger, string tenantId, string tokenId);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Warning,
        Message = "Rejected request: reason={Reason} requestId={RequestId}")]
    public static partial void RequestRejected(ILogger logger, string reason, string requestId);
}
