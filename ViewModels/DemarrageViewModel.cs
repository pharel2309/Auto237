using Auto237.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Auto237.ViewModels;

public partial class DemarrageViewModel : ObservableObject
{
    private readonly AuthService _auth = new();

    [ObservableProperty] bool chargement = true;
    [ObservableProperty] string? erreur;

    [RelayCommand]
    public async Task DeciderAsync()
    {
        try
        {
            Chargement = true;
            Erreur = null;
            // Attend que Shell soit pret (WinUI construit le TabBar en async).
            int attente = 0;
            while (Shell.Current == null && attente < 30)
            {
                await Task.Delay(100);
                attente++;
            }
            await Task.Delay(400);
            bool connecte = false;
            try { connecte = await _auth.EstConnecteAsync(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Demarrage session: {ex.Message}");
                connecte = false;
            }
            var cible = connecte ? "//accueil" : "//connexion";
            bool navigue = false;
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (Shell.Current != null)
                    {
                        await Shell.Current.GoToAsync(cible);
                        navigue = true;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Demarrage nav {cible}: {ex.Message}");
            }
            if (!navigue)
            {
                // Fallback : reessaie connexion une fois, sans lever.
                try
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (Shell.Current != null) await Shell.Current.GoToAsync("//connexion");
                    });
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"Demarrage fallback: {ex2.Message}");
                    Erreur = "Démarrage impossible. Relance l'application.";
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Demarrage: {ex.Message}");
            Erreur = "Démarrage impossible. Relance l'application.";
        }
        finally { Chargement = false; }
    }
}
