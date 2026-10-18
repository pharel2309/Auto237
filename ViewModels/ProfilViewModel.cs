using Auto237.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Auto237.ViewModels;

public partial class ProfilViewModel : ObservableObject
{
    private readonly AuthService _auth = new();
    [ObservableProperty] string email = string.Empty;
    [ObservableProperty] string nom = string.Empty;
    [ObservableProperty] string nomAgence = string.Empty;
    [ObservableProperty] string responsable = string.Empty;
    [ObservableProperty] string role = "client";
    [ObservableProperty] string quota = string.Empty;
    [ObservableProperty] bool estAgence;
    [ObservableProperty] int nbAnnonces;
    [ObservableProperty] int nbFavoris;
    [ObservableProperty] string photoUrl = string.Empty;
    [ObservableProperty] bool chargement;

    public string PhotoAffichee => string.IsNullOrWhiteSpace(PhotoUrl) ? "user.svg" : PhotoUrl;

    public string NomAffiche => EstAgence && !string.IsNullOrWhiteSpace(NomAgence) ? NomAgence : Nom;

    public string RoleAffiche => EstAgence ? "Agence Pro" : "Particulier";

    public bool EstChargementVisible => Chargement && string.IsNullOrWhiteSpace(PhotoUrl);

    [RelayCommand]
    public async Task ChargerAsync()
    {
        var svc = new AnnonceService();
        try
        {
            var annonces = await svc.MesAnnoncesAsync();
            NbAnnonces = annonces.Count;
            var favs = await svc.MesFavorisAsync();
            NbFavoris = favs.Count;
        }
        catch { }
        Email = await SecureStorage.Default.GetAsync("auto237_email") ?? string.Empty;
        Role = await SecureStorage.Default.GetAsync("auto237_role") ?? "client";
        try
        {
            var profil = await svc.MonProfilAsync();
            if (profil != null)
            {
                Role = profil.Role ?? Role;
                Nom = profil.Nom ?? string.Empty;
                NomAgence = profil.NomAgence ?? string.Empty;
                var cacheResp = await SecureStorage.Default.GetAsync("auto237_responsable");
                Responsable = !string.IsNullOrWhiteSpace(cacheResp) ? cacheResp : ExtraireResponsable(Nom);
                PhotoUrl = profil.PhotoUrl ?? string.Empty;
                if (string.IsNullOrWhiteSpace(NomAgence))
                    NomAgence = ExtraireNomAgence(Nom);
                await SecureStorage.Default.SetAsync("auto237_role", Role);
                await SecureStorage.Default.SetAsync("auto237_nom", Nom);
                await SecureStorage.Default.SetAsync("auto237_nomagence", NomAgence);
                await SecureStorage.Default.SetAsync("auto237_telephone", profil.Telephone ?? string.Empty);
                await SecureStorage.Default.SetAsync("auto237_photo", PhotoUrl ?? string.Empty);
                OnPropertyChanged(nameof(PhotoAffichee));
                OnPropertyChanged(nameof(NomAffiche));
                OnPropertyChanged(nameof(RoleAffiche));
                OnPropertyChanged(nameof(EstChargementVisible));
            }
        }
        catch { }
        if (string.IsNullOrWhiteSpace(Nom))
            Nom = await SecureStorage.Default.GetAsync("auto237_nom") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(NomAgence))
            NomAgence = await SecureStorage.Default.GetAsync("auto237_nomagence") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(Responsable))
            Responsable = await SecureStorage.Default.GetAsync("auto237_responsable") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(Responsable))
            Responsable = ExtraireResponsable(Nom);
        if (string.IsNullOrWhiteSpace(PhotoUrl))
            PhotoUrl = await SecureStorage.Default.GetAsync("auto237_photo") ?? string.Empty;
        OnPropertyChanged(nameof(PhotoAffichee));
        OnPropertyChanged(nameof(NomAffiche));
        OnPropertyChanged(nameof(RoleAffiche));
        OnPropertyChanged(nameof(EstChargementVisible));
        EstAgence = string.Equals(Role, "agence", StringComparison.OrdinalIgnoreCase);
        try
        {
            if (EstAgence && string.IsNullOrWhiteSpace(NomAgence))
            {
                var svcBackfill = new AnnonceService();
                await svcBackfill.PatchNomAgenceAsync(NomAgence);
            }
        }
        catch { }
        bool estAgence = EstAgence;
        if (estAgence)
        {
            Quota = "Annonces : illimitées";
        }
        else
        {
            try
            {
                var nbMois = await svc.CompterAnnoncesDuMoisAsync();
                Quota = $"Annonces ce mois : {nbMois}/3";
            }
            catch { Quota = $"Annonces ce mois : {NbAnnonces}/3)"; }
        }
    }

    private static string ExtraireNomAgence(string nomComplet)
    {
        if (string.IsNullOrWhiteSpace(nomComplet)) return string.Empty;
        var idx = nomComplet.LastIndexOf('(');
        if (idx <= 0) return string.Empty;
        return nomComplet.Substring(0, idx).Trim();
    }

    private static string ExtraireResponsable(string nomComplet)
    {
        if (string.IsNullOrWhiteSpace(nomComplet)) return string.Empty;
        var debut = nomComplet.LastIndexOf('(');
        var fin = nomComplet.LastIndexOf(')');
        if (debut < 0 || fin <= debut) return nomComplet.Trim();
        return nomComplet.Substring(debut + 1, fin - debut - 1).Trim();
    }

    [RelayCommand]
    public async Task ChangerPhotoAsync()
    {
        if (Chargement) return;
        Chargement = true;
        try
        {
            var svc = new AnnonceService();
            var url = await svc.ChangerPhotoProfilAsync();
            if (!string.IsNullOrWhiteSpace(url)) { PhotoUrl = url; OnPropertyChanged(nameof(PhotoAffichee)); OnPropertyChanged(nameof(EstChargementVisible)); }
        }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Photo", ex.Message, "OK"); }
        finally { Chargement = false; }
    }

    [RelayCommand]
    public async Task RetourAsync()
    {
        try { await Shell.Current.GoToAsync("//accueil"); }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Navigation", ex.Message, "OK"); }
    }

    [RelayCommand]
    public async Task MesAnnoncesAsync()
    {
        try { await Shell.Current.GoToAsync("//mesannonces"); }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Navigation", ex.Message, "OK"); }
    }

    [RelayCommand]
    public async Task DeconnecterAsync()
    {
        try
        {
            await _auth.DeconnecterAsync();
            await Shell.Current.GoToAsync("//connexion");
        }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Déconnexion", ex.Message, "OK"); }
    }

    [RelayCommand]
    public async Task ModifierTelephoneAsync()
    {
        try { await Shell.Current.GoToAsync("//modifier-telephone"); }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Navigation", ex.Message, "OK"); }
    }

    [RelayCommand]
    public async Task ModifierMotDePasseAsync()
    {
        try { await Shell.Current.GoToAsync("//modifier-motdepasse"); }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Navigation", ex.Message, "OK"); }
    }

    [RelayCommand]
    public async Task ModifierAgenceAsync()
    {
        try { await Shell.Current.GoToAsync("//modifier-agence"); }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Navigation", ex.Message, "OK"); }
    }
}
