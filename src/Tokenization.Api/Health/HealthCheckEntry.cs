using System.Text.Json.Serialization;

namespace Tokenization.Api.Health;

/// <summary>
/// Health check entry.
/// </summary>
internal sealed record HealthCheckEntry
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("duration")]
    public double Duration { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("data")]
    public Dictionary<string, object>? Data { get; init; }
}
