using Auto237.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Auto237.ViewModels;

public partial class RecuperationViewModel : ObservableObject
{
    private readonly AuthService _auth = new();
    [ObservableProperty] string email = string.Empty;
    [ObservableProperty] string? message;
    [ObservableProperty] bool chargement;

    [RelayCommand]
    public async Task EnvoyerAsync()
    {
        Message = null;
        if (Chargement) return;
        if (string.IsNullOrWhiteSpace(Email)) { Message = "Saisis ton email."; return; }
        if (!Services.Validation.EmailValide(Email)) { Message = "Cet email semble invalide, vérifie le @ et le .com — ex. admin@gmail.com"; return; }
        try
        {
            Chargement = true;
            await _auth.EnvoyerLienResetAsync(Email);
            Message = "Lien de réinitialisation envoyé. Vérifie ta boîte mail.";
        }
        catch (Exception ex) { Message = Services.ErreursSupabase.MessageAimable(ex.Message); }
        finally { Chargement = false; }
    }

    [RelayCommand]
    public async Task RetourAsync()
    {
        try { await Shell.Current.GoToAsync("//connexion"); }
        catch (Exception ex) { Message = ex.Message; }
    }
}
