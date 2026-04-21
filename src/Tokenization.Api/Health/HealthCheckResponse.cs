using System.Text.Json.Serialization;

namespace Tokenization.Api.Health;

/// <summary>
/// Health check response model.
/// </summary>
internal sealed record HealthCheckResponse
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("checks")]
    public HealthCheckEntry[] Checks { get; init; } = [];

    [JsonPropertyName("totalDuration")]
    public double TotalDuration { get; init; }
}
