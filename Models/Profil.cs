namespace Auto237.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Profil public lié à auth.users.id (UUID). RLS: lecture publique limitée, écriture owner.
/// </summary>
public class Profil
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    [JsonPropertyName("nom")]
    public string Nom { get; set; } = string.Empty;
    [JsonPropertyName("telephone")]
    public string? Telephone { get; set; }
    [JsonPropertyName("telephone_whatsapp")]
    public string? TelephoneWhatsapp { get; set; }
    [JsonPropertyName("role")]
    public string Role { get; set; } = "client"; // client | agence
    [JsonPropertyName("nom_agence")]
    public string? NomAgence { get; set; }
    [JsonPropertyName("photo_url")]
    public string? PhotoUrl { get; set; }
    [JsonIgnore]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

