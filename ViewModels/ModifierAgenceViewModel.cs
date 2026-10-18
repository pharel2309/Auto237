using Auto237.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Auto237.ViewModels;

public partial class ModifierAgenceViewModel : ObservableObject
{
    private readonly AnnonceService _annonces = new();

    [ObservableProperty] string nomAgence = string.Empty;
    [ObservableProperty] string responsable = string.Empty;
    [ObservableProperty] bool chargement;
    [ObservableProperty] string? erreur;

    [RelayCommand]
    public async Task RetourAsync()
    {
        try { await Shell.Current.GoToAsync("//profil"); }
        catch { }
    }

    public async Task ChargerAsync()
    {
        Erreur = null;
        try
        {
            var profil = await _annonces.MonProfilAsync();
            if (profil == null)
            {
                Erreur = "Impossible de charger tes infos, vérifie ta connexion.";
                return;
            }
            if (!string.Equals(profil.Role, "agence", StringComparison.OrdinalIgnoreCase))
            {
                await Shell.Current.GoToAsync("//profil");
                return;
            }
            NomAgence = profil.NomAgence ?? string.Empty;
            Responsable = await SecureStorage.Default.GetAsync("auto237_responsable") ?? profil.Nom ?? string.Empty;
            await SecureStorage.Default.SetAsync("auto237_nomagence", NomAgence);
            if (string.IsNullOrWhiteSpace(NomAgence))
                Erreur = "Complète le nom de ton agence.";
        }
        catch
        {
            NomAgence = await SecureStorage.Default.GetAsync("auto237_nomagence") ?? string.Empty;
            Responsable = await SecureStorage.Default.GetAsync("auto237_responsable") ?? string.Empty;
            Erreur = "Impossible de charger tes infos, vérifie ta connexion.";
        }
    }

    [RelayCommand]
    public async Task EnregistrerAsync()
    {
        Erreur = null;
        if (Chargement) return;
        if (string.IsNullOrWhiteSpace(NomAgence) || string.IsNullOrWhiteSpace(Responsable))
        {
            Erreur = "Nom de l'agence et responsable sont obligatoires.";
            return;
        }
        Chargement = true;
        try
        {
            var nom = await SecureStorage.Default.GetAsync("auto237_nom") ?? string.Empty;
            var telephone = await _annonces.TelephoneProfilAsync() ?? string.Empty;
            string? waExistant = null;
            try { waExistant = (await _annonces.MonProfilAsync())?.TelephoneWhatsapp; } catch { }
            var (ok, err) = await _annonces.ModifierProfilAsync(telephone, waExistant, nom, NomAgence.Trim(), true);
            if (!ok) { Erreur = err; return; }
            await SecureStorage.Default.SetAsync("auto237_nomagence", NomAgence.Trim());
            await SecureStorage.Default.SetAsync("auto237_responsable", Responsable.Trim());
            await Shell.Current.DisplayAlertAsync("Modification", "Infos agence mises à jour.", "OK");
            await Shell.Current.GoToAsync("//profil");
        }
        catch (Exception ex) { Erreur = ex.Message; }
        finally { Chargement = false; }
    }
}
