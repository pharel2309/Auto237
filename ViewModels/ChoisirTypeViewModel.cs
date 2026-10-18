using Auto237.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Auto237.ViewModels;

public partial class ChoisirTypeViewModel : ObservableObject
{
    private readonly AnnonceService _annonces = new();

    [ObservableProperty] string quotaInfo = string.Empty;
    [ObservableProperty] string? message;

    [RelayCommand]
    public async Task ChargerAsync()
    {
        try
        {
            var role = await SecureStorage.Default.GetAsync("auto237_role") ?? "client";
            if (string.Equals(role, "agence", StringComparison.OrdinalIgnoreCase))
                QuotaInfo = "Compte agence : publications illimitées.";
            else
            {
                var nb = await _annonces.CompterAnnoncesDuMoisAsync();
                QuotaInfo = $"Compte particulier : {nb}/3 annonces ce mois.";
            }
        }
        catch { QuotaInfo = string.Empty; }
    }

    [RelayCommand]
    async Task ChoisirVenteAsync()
    {
        try { await Shell.Current.GoToAsync("//publier-form?type=Vente"); }
        catch (Exception ex) { Message = ex.Message; }
    }

    [RelayCommand]
    async Task ChoisirLocationAsync()
    {
        try { await Shell.Current.GoToAsync("//publier-form?type=Location"); }
        catch (Exception ex) { Message = ex.Message; }
    }
}
