using Auto237.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Auto237.ViewModels;

public partial class ModifierMotDePasseViewModel : ObservableObject
{
    private readonly AuthService _auth = new();

    [ObservableProperty] string ancienMotDePasse = string.Empty;
    [ObservableProperty] string nouveauMotDePasse = string.Empty;
    [ObservableProperty] string confirmationMotDePasse = string.Empty;
    [ObservableProperty] bool chargement;
    [ObservableProperty] string? erreur;

    [RelayCommand]
    public async Task RetourAsync()
    {
        try { await Shell.Current.GoToAsync("//profil"); }
        catch { }
    }

    [RelayCommand]
    public async Task ChangerAsync()
    {
        Erreur = null;
        if (Chargement) return;
        if (string.IsNullOrWhiteSpace(AncienMotDePasse) || string.IsNullOrWhiteSpace(NouveauMotDePasse) || string.IsNullOrWhiteSpace(ConfirmationMotDePasse))
        {
            Erreur = "Tous les champs de mot de passe sont obligatoires.";
            return;
        }
        if (NouveauMotDePasse.Length < 6) { Erreur = "Mot de passe : 6 caractères minimum."; return; }
        if (NouveauMotDePasse != ConfirmationMotDePasse) { Erreur = "Les mots de passe ne correspondent pas."; return; }
        Chargement = true;
        try
        {
            var (ok, err) = await _auth.ChangePasswordAsync(AncienMotDePasse, NouveauMotDePasse);
            if (!ok) { Erreur = err; return; }
            await Shell.Current.DisplayAlertAsync("Modification", "Mot de passe changé. Reconnecte-toi.", "OK");
            await _auth.DeconnecterAsync();
            await Shell.Current.GoToAsync("//connexion");
        }
        catch (Exception ex) { Erreur = ex.Message; }
        finally { Chargement = false; }
    }
}
