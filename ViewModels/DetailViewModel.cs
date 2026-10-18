using Auto237.Models;
using Auto237.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Web;

namespace Auto237.ViewModels;

[QueryProperty(nameof(VehiculeId), "id")]
public partial class DetailViewModel : ObservableObject
{
    private readonly AnnonceService _annonces = new();

    [ObservableProperty] string vehiculeId = string.Empty;
    [ObservableProperty] Vehicule? vehicule;
    [ObservableProperty] int position;
    [ObservableProperty] bool chargement;
    [ObservableProperty] string? erreur;
    [ObservableProperty] bool estProprietaire;

    public bool EstVendu =>
        Vehicule != null && (string.Equals(Vehicule.Statut, "vendu", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Vehicule.Statut, "loue", StringComparison.OrdinalIgnoreCase));
    public bool EstVenduTiers => EstVendu && !EstProprietaire;
    public bool ContactVisible => !EstProprietaire && !EstVenduTiers;
    public string TexteStatut =>
        Vehicule == null ? string.Empty
        : string.Equals(Vehicule.Statut, "loue", StringComparison.OrdinalIgnoreCase) ? "Annonce louee"
        : "Annonce vendue";
    public string TexteStatutAction =>
        EstVendu ? "Remettre en ligne"
        : Vehicule != null && string.Equals(Vehicule.TypeOffre, "location", StringComparison.OrdinalIgnoreCase) ? "Marquer loue" : "Marquer vendu";

    public bool AWhatsApp => Vehicule != null && !string.IsNullOrWhiteSpace(Vehicule.TelWhatsapp);

    public string NomVendeurAffiche =>
        Vehicule == null ? string.Empty
        : Vehicule.EstAgence && !string.IsNullOrWhiteSpace(Vehicule.NomAgenceVendeur) ? Vehicule.NomAgenceVendeur
        : !string.IsNullOrWhiteSpace(Vehicule.NomVendeur) ? Vehicule.NomVendeur.Trim()
        : "Vendeur particulier";

    public string PhotoVendeurAffichee =>
        Vehicule == null || string.IsNullOrWhiteSpace(Vehicule.PhotoVendeur) ? "user.svg" : Vehicule.PhotoVendeur;

    private string _dernierId = string.Empty;

    public ObservableCollection<string> PhotosUrls { get; } = new();

    partial void OnVehiculeIdChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
            _ = ChargerAsync();
    }

    [RelayCommand]
    public async Task ChargerAsync()
    {
        if (string.IsNullOrEmpty(VehiculeId)) return;
        if (VehiculeId == _dernierId) return;
        _dernierId = VehiculeId;
        try
        {
            Chargement = true; Erreur = null;
            var v = await _annonces.DetailAsync(VehiculeId);
            Vehicule = v;
            Position = 0;
            try
            {
                var uid = await SupabaseService.Instance.UserIdAsync();
                EstProprietaire = v != null && !string.IsNullOrEmpty(uid) && string.Equals(uid, v.UserId, StringComparison.OrdinalIgnoreCase);
            }
            catch { EstProprietaire = false; }
            OnPropertyChanged(nameof(AWhatsApp));
            OnPropertyChanged(nameof(NomVendeurAffiche));
            OnPropertyChanged(nameof(PhotoVendeurAffichee));
            OnPropertyChanged(nameof(EstVendu));
            OnPropertyChanged(nameof(EstVenduTiers));
            OnPropertyChanged(nameof(ContactVisible));
            OnPropertyChanged(nameof(TexteStatut));
            OnPropertyChanged(nameof(TexteStatutAction));
            PhotosUrls.Clear();
            if (v != null) foreach (var p in v.Photos) PhotosUrls.Add(p.Url);
            if (v == null) Erreur = "Annonce introuvable.";
        }
        catch (Exception ex) { Erreur = ex.Message; }
        finally { Chargement = false; }
    }

    [RelayCommand]
    public async Task RetourAsync()
    {
        try { await Shell.Current.GoToAsync("//accueil"); }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Navigation", ex.Message, "OK"); }
    }

    [RelayCommand]
    public async Task ModifierAsync()
    {
        if (Vehicule == null || string.IsNullOrEmpty(Vehicule.Id)) return;
        try { await Shell.Current.GoToAsync($"//publier-form?id={Vehicule.Id}"); }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Navigation", ex.Message, "OK"); }
    }

    [RelayCommand]
    public async Task ChangerStatutAsync()
    {
        if (Vehicule == null || Chargement) return;
        try
        {
            Chargement = true;
            if (EstVendu)
            {
                bool ok = await Shell.Current.DisplayAlertAsync("Remettre en ligne", $"Remettre '{Vehicule.Titre}' en ligne ?", "Oui", "Non");
                if (!ok) return;
                await _annonces.ChangerStatutAsync(Vehicule.Id, "publie");
                Vehicule.Statut = "publie";
            }
            else
            {
                var cible = string.Equals(Vehicule.TypeOffre, "location", StringComparison.OrdinalIgnoreCase) ? "loue" : "vendu";
                bool ok = await Shell.Current.DisplayAlertAsync("Statut", $"Marquer comme {cible} ?", "Oui", "Non");
                if (!ok) return;
                await _annonces.ChangerStatutAsync(Vehicule.Id, cible);
                Vehicule.Statut = cible;
            }
            var copie = Vehicule; Vehicule = null; Vehicule = copie;
            OnPropertyChanged(nameof(EstVendu)); OnPropertyChanged(nameof(EstVenduTiers));
            OnPropertyChanged(nameof(ContactVisible)); OnPropertyChanged(nameof(TexteStatut)); OnPropertyChanged(nameof(TexteStatutAction));
        }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Statut", ex.Message, "OK"); }
        finally { Chargement = false; }
    }

    [RelayCommand]
    public async Task BasculerFavoriAsync()
    {
        if (Vehicule == null) return;
        try
        {
            var nouvelEtat = !Vehicule.EstFavori;
            await _annonces.BasculerFavoriAsync(Vehicule.Id, nouvelEtat);
            Vehicule.EstFavori = nouvelEtat;
            var copie = Vehicule;
            Vehicule = null;
            Vehicule = copie;
        }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Favoris", ex.Message, "OK"); }
    }

    [RelayCommand]
    public async Task AppelerAsync()
    {
        try
        {
            if (Vehicule == null || string.IsNullOrWhiteSpace(Vehicule.TelContact)) return;
            if (PhoneDialer.Default.IsSupported) PhoneDialer.Default.Open(Vehicule.TelContact.Trim());
        }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Appel", ex.Message, "OK"); }
    }

    [RelayCommand]
    public async Task WhatsAppAsync()
    {
        try
        {
            if (Vehicule == null || string.IsNullOrWhiteSpace(Vehicule.TelWhatsapp)) return;
            var tel = new string(Vehicule.TelWhatsapp.Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(tel)) return;
            await Launcher.Default.OpenAsync($"https://wa.me/{tel}?text={HttpUtility.UrlEncode($"Bonjour, je suis intéressé par votre annonce : {Vehicule.Titre}")}");
        }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("WhatsApp", ex.Message, "OK"); }
    }
}
