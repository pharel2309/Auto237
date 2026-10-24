using System.Text.Json.Serialization;

namespace Auto237.Models;

/// <summary>
/// Annonce véhicule — fidèle aux cartes Figma (titre bleu, prix rouge, badge catégorie).
/// </summary>
public class Vehicule
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("titre")]
    public string Titre { get; set; } = string.Empty;

    [JsonPropertyName("marque")]
    public string Marque { get; set; } = string.Empty;

    [JsonPropertyName("modele")]
    public string? Modele { get; set; }

    [JsonPropertyName("annee")]
    public int Annee { get; set; } = DateTime.Now.Year;

    [JsonPropertyName("ville")]
    public string Ville { get; set; } = "Douala";

    [JsonPropertyName("boite")]
    public string Boite { get; set; } = "manuelle"; // manuelle | auto

    [JsonPropertyName("type_offre")]
    public string TypeOffre { get; set; } = "vente"; // vente | location

    [JsonPropertyName("prix_vente")]
    public long? PrixVente { get; set; }

    [JsonPropertyName("prix_jour")]
    public long? PrixJour { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("tel_contact")]
    public string TelContact { get; set; } = string.Empty;

    [JsonPropertyName("tel_whatsapp")]
    public string? TelWhatsapp { get; set; }

    [JsonPropertyName("statut")]
    public string Statut { get; set; } = "publie";

    [JsonPropertyName("vues")]
    public int Vues { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Non mappé : jointures locales (ignorés par Supabase, sinon PGRST204)
    [JsonIgnore] public List<Photo> Photos { get; set; } = new();
    [JsonIgnore] public bool EstFavori { get; set; }
    [JsonIgnore] public string RoleVendeur { get; set; } = "client";
    [JsonIgnore] public string? NomVendeur { get; set; }
    [JsonIgnore] public string? NomAgenceVendeur { get; set; }
    [JsonIgnore] public string? PhotoVendeur { get; set; }
    [JsonIgnore] public bool EstAgence => string.Equals(RoleVendeur, "agence", StringComparison.OrdinalIgnoreCase);
    [JsonIgnore]
    public bool EstVendu =>
        string.Equals(Statut, "vendu", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Statut, "loue", StringComparison.OrdinalIgnoreCase);
    [JsonIgnore]
    public string TexteStatutAction =>
        EstVendu ? "Remettre en ligne"
        : string.Equals(TypeOffre, "location", StringComparison.OrdinalIgnoreCase) ? "Marquer comme loué"
        : "Marquer comme vendu";
    [JsonIgnore]
    public string StatutAffiche =>
        string.Equals(Statut, "loue", StringComparison.OrdinalIgnoreCase) ? "Loue"
        : string.Equals(Statut, "vendu", StringComparison.OrdinalIgnoreCase) ? "Vendu"
        : string.Empty;
    [JsonIgnore]
    public string BadgeVendeur =>
        EstAgence ? $"● {(string.IsNullOrWhiteSpace(NomAgenceVendeur) ? "Agence" : NomAgenceVendeur)} — Pro"
        : string.IsNullOrWhiteSpace(NomVendeur) ? "Particulier" : $"{NomVendeur.Trim()} — Particulier";

    [JsonIgnore]
    public string PrixAffiche =>
        TypeOffre == "location" && PrixJour.HasValue
            ? $"{PrixJour.Value:N0} FCFA/jour".Replace(",", " ")
            : $"{(PrixVente ?? 0):N0} FCFA".Replace(",", " ");

    [JsonIgnore]
    public string PhotoPrincipale =>
        Photos.FirstOrDefault()?.Url ?? "placeholder_car.svg";

    [JsonIgnore]
    public string SpecsAffiche
    {
        get
        {
            var parts = new List<string>();
            if (Annee > 0) parts.Add(Annee.ToString());
            if (!string.IsNullOrWhiteSpace(Boite))
                parts.Add(string.Equals(Boite, "auto", StringComparison.OrdinalIgnoreCase) ? "Automatique" : "Manuelle");
            if (!string.IsNullOrWhiteSpace(Ville)) parts.Add(Ville.Trim());
            return string.Join(" • ", parts);
        }
    }
}

