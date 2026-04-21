namespace Tokenization.Api.Config.Options;

/// <summary>
/// Request size limiting configuration options for security hardening.
/// </summary>
internal sealed class RequestSizeOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "RequestSize";
    
    /// <summary>
    /// Maximum request body size in bytes. Default is 32KB for payment APIs.
    /// </summary>
    public long MaxRequestBodySize { get; init; } = 32 * 1024; // 32KB

    /// <summary>
    /// Maximum multipart body length in bytes. Default is 32KB.
    /// </summary>
    public long MultipartBodyLengthLimit { get; init; } = 32 * 1024; // 32KB

    /// <summary>
    /// Maximum form value length in bytes. Default is 4KB.
    /// </summary>
    public int MaxFormValueLength { get; init; } = 4 * 1024; // 4KB

    /// <summary>
    /// Maximum form key length in bytes. Default is 1KB.
    /// </summary>
    public int MaxFormKeyLength { get; init; } = 1024; // 1KB

    /// <summary>
    /// Maximum query string value length in bytes. Default is 1KB.
    /// </summary>
    public int ValueLengthLimit { get; init; } = 1024; // 1KB

    /// <summary>
    /// Maximum number of form fields. Default is 10.
    /// </summary>
    public int MaxFormFieldCount { get; init; } = 10;
}