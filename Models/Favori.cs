using System.Text.Json.Serialization;

namespace Auto237.Models;

public class Favori
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("vehicule_id")]
    public string VehiculeId { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

