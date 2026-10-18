using Auto237.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Auto237.ViewModels;

public partial class InscriptionViewModel : ObservableObject
{
    private readonly AuthService _auth = new();

    [ObservableProperty] string nom = string.Empty;
    [ObservableProperty] string nomAgence = string.Empty;
    [ObservableProperty] string email = string.Empty;
    [ObservableProperty] string telephone = string.Empty;
    [ObservableProperty] string password = string.Empty;
    [ObservableProperty] string confirmation = string.Empty;
    [ObservableProperty] bool chargement;
    [ObservableProperty] string? erreur;
    [ObservableProperty] bool estAgence;

    public bool EstParticulier => !EstAgence;

    partial void OnEstAgenceChanged(bool value) => OnPropertyChanged(nameof(EstParticulier));

    [RelayCommand]
    public async Task ChoisirParticulierAsync() { EstAgence = false; }

    [RelayCommand]
    public async Task ChoisirAgenceAsync() { EstAgence = true; }

    [RelayCommand]
    public async Task InscrireAsync()
    {
        Erreur = null;
        if (Chargement) return;
        Email = Validation.NormaliserEmail(Email);
        if (EstAgence)
        {
            if (string.IsNullOrWhiteSpace(NomAgence) || string.IsNullOrWhiteSpace(Nom) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Telephone) || string.IsNullOrWhiteSpace(Password))
            {
                Erreur = "Tous les champs sont obligatoires (nom agence, responsable, email, téléphone, mot de passe).";
                return;
            }
        }
        else if (string.IsNullOrWhiteSpace(Nom) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Telephone) || string.IsNullOrWhiteSpace(Password))
        {
            Erreur = "Tous les champs sont obligatoires (nom, email, téléphone, mot de passe).";
            return;
        }
        if (!Validation.EmailValide(Email))
        {
            Erreur = "Cet email semble invalide, vérifie le @ et le .com — ex. admin@gmail.com";
            return;
        }
        if (Auto237.Config.SupabaseConfig.RestrictionPointsEmail && !Validation.EmailAccepte(Email))
        {
            Erreur = "Les points ne sont pas acceptés avant le @ sur ce service — ex. particuliertest@gmail.com";
            return;
        }
        if (Password != Confirmation) { Erreur = "Les mots de passe ne correspondent pas."; return; }
        if (Password.Length < 6) { Erreur = "Mot de passe : 6 caractères minimum."; return; }
        try
        {
            Chargement = true;
            string role = EstAgence ? "agence" : "client";
            string nomEnvoi = EstAgence ? $"{NomAgence.Trim()} ({Nom.Trim()})" : Nom;
            string? nomAg = EstAgence ? NomAgence : null;
            var (ok, err) = await _auth.InscrireAsync(nomEnvoi, Email, Password, Telephone, role, nomAg);
            if (!ok) { Erreur = ErreursSupabase.MessageAimable(err ?? string.Empty); return; }
            await Shell.Current.GoToAsync("//accueil");
        }
        finally { Chargement = false; }
    }

    [RelayCommand]
    public async Task RetourConnexionAsync()
    {
        try { await Shell.Current.GoToAsync("//connexion"); }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Navigation", ex.Message, "OK"); }
    }
}
