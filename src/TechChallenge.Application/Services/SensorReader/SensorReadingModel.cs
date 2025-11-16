using System.Text.Json.Serialization;

namespace TechChallenge.Application.Services.SensorReader;

/// <summary>
/// Represents a single sensor reading from JSON
/// </summary>
public class SensorReadingModel
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("zone")]
    public string Zone { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public float Value { get; set; }

    /// <summary>
    /// Indicates if this sensor reading is valid for processing
    /// </summary>
    [JsonIgnore]
    public bool IsValid => !string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(Zone) && !float.IsNaN(Value);
}
