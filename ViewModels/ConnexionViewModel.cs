using Auto237.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Auto237.ViewModels;

public partial class ConnexionViewModel : ObservableObject
{
    private readonly AuthService _auth = new();

    [ObservableProperty] string email = string.Empty;
    [ObservableProperty] string password = string.Empty;
    [ObservableProperty] bool chargement;
    [ObservableProperty] string? erreur;

    [RelayCommand]
    public async Task ConnecterAsync()
    {
        Erreur = null;
        if (Chargement) return;
        Email = Validation.NormaliserEmail(Email);
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            Erreur = "Saisis ton email et ton mot de passe.";
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
        try
        {
            Chargement = true;
            var (ok, err) = await _auth.ConnecterAsync(Email, Password);
            if (!ok) { Erreur = ErreursSupabase.MessageAimable(err ?? string.Empty); return; }
            await Shell.Current.GoToAsync("//accueil");
        }
        finally { Chargement = false; }
    }

    [RelayCommand]
    public async Task AllerInscriptionAsync()
    {
        try { await Shell.Current.GoToAsync("//inscription"); }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Navigation", ex.Message, "OK"); }
    }

    [RelayCommand]
    public async Task AllerRecuperationAsync()
    {
        try { await Shell.Current.GoToAsync("//recuperation"); }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Navigation", ex.Message, "OK"); }
    }
}
