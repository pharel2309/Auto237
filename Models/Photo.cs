using System.Text.Json.Serialization;

namespace Auto237.Models;

public class Photo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("vehicule_id")]
    public string VehiculeId { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("ordre")]
    public int Ordre { get; set; }
}

