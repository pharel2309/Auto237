using Auto237.Models;
using Auto237.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Auto237.ViewModels;

public partial class PublierViewModel : ObservableObject, IQueryAttributable
{
    private readonly AnnonceService _annonces = new();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("type", out var t) && t is string s && !string.IsNullOrWhiteSpace(s))
            TypeOffre = string.Equals(s, "Location", StringComparison.OrdinalIgnoreCase) ? "Location" : "Vente";
        if (query.TryGetValue("id", out var i) && i is string sid && !string.IsNullOrWhiteSpace(sid))
        {
            EstEdition = true; AnnonceId = sid.Trim();
            _ = ChargerAnnonceAsync();
        }
        else { EstEdition = false; AnnonceId = string.Empty; }
        OnPropertyChanged(nameof(EstEdition));
        OnPropertyChanged(nameof(TitrePage));
        OnPropertyChanged(nameof(TexteBouton));
    }

    [ObservableProperty] bool estEdition;
    [ObservableProperty] string annonceId = string.Empty;
    public string TitrePage => EstEdition ? "Modifier l'annonce" : "Publier une annonce";
    public string TexteBouton => EstEdition ? "Enregistrer" : "Publier";

    [ObservableProperty] string marque = string.Empty;
    [ObservableProperty] string modele = string.Empty;
    [ObservableProperty] string annee = string.Empty;
    [ObservableProperty] string ville = "Douala";
    [ObservableProperty] string villeSelection = "Douala";
    [ObservableProperty] string villeAutre = string.Empty;
    [ObservableProperty] bool estManuel = true;
    [ObservableProperty] bool estAutomatique;
    [ObservableProperty] string typeOffre = "Vente";
    [ObservableProperty] string prixVente = string.Empty;
    [ObservableProperty] string prixJour = string.Empty;
    [ObservableProperty] string description = string.Empty;
    [ObservableProperty] bool chargement;
    [ObservableProperty] string? message;
    [ObservableProperty] string quotaInfo = string.Empty;

    public List<string> Villes { get; } = new() { "Douala", "Yaoundé", "Bafoussam", "Kribi", "Garoua", "Bamenda", "Autre" };
    public ObservableCollection<string> PhotosLocales { get; } = new();
    public List<string> PhotosSupprimees { get; } = new();

    [RelayCommand]
    public async Task ChargerAnnonceAsync()
    {
        if (string.IsNullOrWhiteSpace(AnnonceId)) return;
        try
        {
            Chargement = true; Message = null;
            var v = await _annonces.DetailAsync(AnnonceId.Trim(), incrementerVues: false);
            if (v == null) { Message = "Annonce introuvable."; return; }
            Marque = v.Marque ?? string.Empty; Modele = v.Modele ?? string.Empty;
            Annee = v.Annee > 0 ? v.Annee.ToString() : string.Empty;
            TypeOffre = string.Equals(v.TypeOffre, "location", StringComparison.OrdinalIgnoreCase) ? "Location" : "Vente";
            PrixVente = v.PrixVente?.ToString() ?? string.Empty; PrixJour = v.PrixJour?.ToString() ?? string.Empty;
            Description = v.Description ?? string.Empty;
            if (Villes.Contains(v.Ville)) { VilleSelection = v.Ville; VilleAutre = string.Empty; }
            else { VilleSelection = "Autre"; VilleAutre = v.Ville ?? string.Empty; }
            EstAutomatique = string.Equals(v.Boite, "auto", StringComparison.OrdinalIgnoreCase);
            EstManuel = !EstAutomatique;
            PhotosLocales.Clear(); PhotosSupprimees.Clear();
            foreach (var p in v.Photos) PhotosLocales.Add(p.Url);
            NotifierPhotos();
        }
        catch (Exception ex) { Message = ex.Message; }
        finally { Chargement = false; }
    }

    public bool HasPhotos => PhotosLocales.Count > 0;
    public double PhotosHauteur => PhotosLocales.Count == 0 ? 0 : 100;
    public string CompteurPhotos => PhotosLocales.Count == 0 ? string.Empty : $"Photos : {PhotosLocales.Count} ajoutée(s)";

    public string TelAffiche { get; set; } = string.Empty;
    public string TelWhatsappAffiche { get; set; } = string.Empty;

    public bool EstVente => TypeOffre == "Vente";
    partial void OnTypeOffreChanged(string value) => OnPropertyChanged(nameof(EstVente));

    public bool EstVilleAutre => string.Equals(VilleSelection, "Autre", StringComparison.Ordinal);
    public string VilleFinale => (EstVilleAutre ? (VilleAutre ?? string.Empty) : (VilleSelection ?? string.Empty)).Trim();

    partial void OnEstManuelChanged(bool value)
    {
        if (value && EstAutomatique) EstAutomatique = false;
        if (!value && !EstAutomatique) EstAutomatique = true;
    }

    partial void OnEstAutomatiqueChanged(bool value)
    {
        if (value && EstManuel) EstManuel = false;
        if (!value && !EstManuel) EstManuel = true;
    }

    partial void OnVilleSelectionChanged(string value)
    {
        Ville = VilleFinale;
        OnPropertyChanged(nameof(EstVilleAutre));
        OnPropertyChanged(nameof(VilleFinale));
        OnPropertyChanged(nameof(TitreAuto));
    }

    partial void OnVilleAutreChanged(string value)
    {
        Ville = VilleFinale;
        OnPropertyChanged(nameof(VilleFinale));
        OnPropertyChanged(nameof(TitreAuto));
    }
    partial void OnAnneeChanged(string value) => OnPropertyChanged(nameof(TitreAuto));

    public string TitreAuto
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Marque)) parts.Add((Marque ?? string.Empty).Trim());
            if (!string.IsNullOrWhiteSpace(Modele)) parts.Add((Modele ?? string.Empty).Trim());
            if (int.TryParse((Annee ?? string.Empty).Trim(), out var an)) parts.Add(an.ToString());
            var baseTitre = parts.Count > 0 ? string.Join(" ", parts) : "Vehicule";
            return baseTitre;
        }
    }

    [RelayCommand]
    async Task RetourChoixAsync()
    {
        try { await Shell.Current.GoToAsync("//publier"); }
        catch (Exception ex) { Message = ex.Message; }
    }

    [RelayCommand]
    public async Task RafraichirQuotaAsync()
    {
        try
        {
            var role = await SecureStorage.Default.GetAsync("auto237_role") ?? "client";
            bool estAgence = string.Equals(role, "agence", StringComparison.OrdinalIgnoreCase);
            if (estAgence) { QuotaInfo = "Compte agence : publications illimitées."; return; }
            var nb = await _annonces.CompterAnnoncesDuMoisAsync();
            QuotaInfo = $"Compte client : {nb}/3 annonces ce mois.";
        }
        catch { QuotaInfo = "Compte client : 3 annonces par mois."; }
    }

    [RelayCommand]
    public async Task AjouterPhotoAsync()
    {
        if (Chargement) return;
        Message = null;
        try
        {
            var role = await SecureStorage.Default.GetAsync("auto237_role") ?? "client";
            int maxPhotos = string.Equals(role, "agence", StringComparison.OrdinalIgnoreCase) ? 5 : 3;
            if (PhotosLocales.Count >= maxPhotos)
            {
                Message = maxPhotos == 5 ? "5 photos maximum." : "3 photos maximum pour un compte client (5 pour une agence).";
                return;
            }
            var results = await MediaPicker.Default.PickPhotosAsync();
            if (results != null)
            {
                foreach (var result in results)
                {
                    if (PhotosLocales.Count >= maxPhotos)
                    {
                        Message = maxPhotos == 5 ? "5 photos maximum." : "3 photos maximum pour un compte client (5 pour une agence).";
                        break;
                    }

                    PhotosLocales.Add(result.FullPath);
                }

                NotifierPhotos();
            }
        }
        catch (Exception ex) { Message = ex.Message; }
    }

    [RelayCommand]
    public void RetirerPhoto(string chemin)
    {
        try
        {
            if (!string.IsNullOrEmpty(chemin) && PhotosLocales.Contains(chemin))
            {
                PhotosLocales.Remove(chemin);
                if (EstEdition) PhotosSupprimees.Add(chemin);
                Message = null;
                NotifierPhotos();
            }
        }
        catch (Exception ex) { Message = ex.Message; }
    }

    private void NotifierPhotos()
    {
        OnPropertyChanged(nameof(HasPhotos));
        OnPropertyChanged(nameof(PhotosHauteur));
        OnPropertyChanged(nameof(CompteurPhotos));
    }

    [RelayCommand]
    public async Task PublierAsync()
    {
        Message = null;
        if (Chargement) return;
        if (string.IsNullOrWhiteSpace(Marque))
        {
            Message = "La marque est obligatoire.";
            return;
        }
        if (string.IsNullOrWhiteSpace(VilleFinale))
        {
            Message = EstVilleAutre ? "Précise la ville." : "Choisis une ville.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Annee)) { Message = "L'année est obligatoire."; return; }
        if (!int.TryParse((Annee ?? string.Empty).Trim(), out var anneeVal) || anneeVal < 1980 || anneeVal > 2030) { Message = "Année invalide (1980–2030)."; return; }
        if (EstVente && !long.TryParse(PrixVente, out _)) { Message = "Prix de vente invalide."; return; }
        if (!EstVente && !long.TryParse(PrixJour, out _)) { Message = "Prix par jour invalide."; return; }

        try
        {
            Chargement = true;
            // Téléphone récupéré du profil connecté (pas de resaisie)
            TelAffiche = await _annonces.TelephoneProfilAsync() ?? string.Empty;
            try
            {
                var profil = await _annonces.MonProfilAsync();
                TelWhatsappAffiche = profil?.TelephoneWhatsapp ?? string.Empty;
            }
            catch { TelWhatsappAffiche = string.Empty; }
            if (string.IsNullOrWhiteSpace(TelAffiche))
            {
                Message = "Ton numéro est manquant dans ton profil. Va dans Mon compte pour l'ajouter.";
                return;
            }
            // Quota client : 3 annonces par mois calendaire, 3 photos max. Agence : illimité, 5 photos.
            var role = await SecureStorage.Default.GetAsync("auto237_role") ?? "client";
            bool estAgence = string.Equals(role, "agence", StringComparison.OrdinalIgnoreCase);
            int maxPhotos = estAgence ? 5 : 3;
            if (!estAgence)
            {
                try
                {
                    var nbMois = await _annonces.CompterAnnoncesDuMoisAsync();
                    if (nbMois >= 3)
                    {
                        Message = "Limite atteinte : 3 annonces par mois pour un compte particulier. Passe en compte agence pour publier sans limite.";
                        return;
                    }
                }
                catch { }
            }
            if (PhotosLocales.Count > maxPhotos)
            {
                Message = estAgence ? "5 photos maximum." : "3 photos maximum pour un compte particulier (5 pour une agence).";
                return;
            }
            if (EstEdition)
            {
                await EnregistrerModificationsAsync(role);
                return;
            }
            var v = new Vehicule
            {
                Titre = TitreAuto, Marque = (Marque ?? string.Empty).Trim(), Modele = (Modele ?? string.Empty).Trim(),
                Annee = int.Parse((Annee ?? string.Empty).Trim()), Ville = VilleFinale, Boite = EstAutomatique ? "auto" : "manuelle", TypeOffre = (TypeOffre ?? "Vente").ToLower(),
                PrixVente = EstVente ? long.Parse(PrixVente) : null,
                PrixJour = !EstVente ? long.Parse(PrixJour) : null,
                Description = (Description ?? string.Empty).Trim(),
                TelContact = (TelAffiche ?? string.Empty).Trim(),
                TelWhatsapp = string.IsNullOrWhiteSpace(TelWhatsappAffiche) ? null : TelWhatsappAffiche.Trim()
            };
            var cree = await _annonces.PublierAsync(v, PhotosLocales.ToList());
            try { await Shell.Current.GoToAsync($"detail?id={cree.Id}"); }
            catch (Exception ex) { Message = ex.Message; }
        }
        catch (Exception ex) { Message = ex.Message; }
        finally { Chargement = false; }
    }

    private async Task EnregistrerModificationsAsync(string role)
    {
        var v = new Vehicule
        {
            Id = AnnonceId.Trim(),
            Titre = TitreAuto, Marque = (Marque ?? string.Empty).Trim(), Modele = (Modele ?? string.Empty).Trim(),
            Annee = int.Parse((Annee ?? string.Empty).Trim()), Ville = VilleFinale, Boite = EstAutomatique ? "auto" : "manuelle", TypeOffre = (TypeOffre ?? "Vente").ToLower(),
            PrixVente = EstVente ? long.Parse(PrixVente) : null,
            PrixJour = !EstVente ? long.Parse(PrixJour) : null,
            Description = (Description ?? string.Empty).Trim(),
            TelContact = (TelAffiche ?? string.Empty).Trim(),
            TelWhatsapp = string.IsNullOrWhiteSpace(TelWhatsappAffiche) ? null : TelWhatsappAffiche.Trim()
        };
        await _annonces.ModifierAnnonceAsync(v.Id, v);
        foreach (var sup in PhotosSupprimees.ToList()) await _annonces.SupprimerPhotoParUrlAsync(sup);
        PhotosSupprimees.Clear();
        bool estAgence = string.Equals(role, "agence", StringComparison.OrdinalIgnoreCase);
        int maxPhotos = estAgence ? 5 : 3;
        var nouvelles = PhotosLocales.Where(x => !x.StartsWith("http", StringComparison.OrdinalIgnoreCase)).Take(maxPhotos).ToList();
        foreach (var n in nouvelles) await _annonces.AjouterPhotoAsync(v.Id, n);
        try { await Shell.Current.GoToAsync("//mesannonces"); }
        catch (Exception ex) { Message = ex.Message; }
    }
}
