using Auto237.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Auto237.ViewModels;

public partial class ModifierTelephoneViewModel : ObservableObject
{
    private readonly AnnonceService _annonces = new();

    [ObservableProperty] string telephone = string.Empty;
    [ObservableProperty] string telephoneWhatsapp = string.Empty;
    [ObservableProperty] bool estMemeNumero;
    [ObservableProperty] bool chargement;
    [ObservableProperty] string? erreur;

    public bool EstChampWhatsappVisible => !EstMemeNumero;

    partial void OnEstMemeNumeroChanged(bool value)
    {
        if (value) TelephoneWhatsapp = Telephone;
        else TelephoneWhatsapp = string.Empty;
        OnPropertyChanged(nameof(EstChampWhatsappVisible));
    }

    partial void OnTelephoneChanged(string value)
    {
        if (EstMemeNumero) TelephoneWhatsapp = value;
    }

    [RelayCommand]
    public async Task RetourAsync()
    {
        try { await Shell.Current.GoToAsync("//profil"); }
        catch { }
    }

    public async Task ChargerAsync()
    {
        Erreur = null;
        EstMemeNumero = false;
        TelephoneWhatsapp = string.Empty;
        OnPropertyChanged(nameof(EstChampWhatsappVisible));
        try
        {
            var profil = await _annonces.MonProfilAsync();
            if (profil != null)
            {
                Telephone = profil.Telephone ?? string.Empty;
                var wa = profil.TelephoneWhatsapp ?? string.Empty;
                await SecureStorage.Default.SetAsync("auto237_telephone", Telephone);
                await SecureStorage.Default.SetAsync("auto237_nom", profil.Nom ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(wa) && wa.Trim() == Telephone.Trim() && !string.IsNullOrWhiteSpace(Telephone))
                {
                    EstMemeNumero = true;
                    TelephoneWhatsapp = Telephone;
                }
                else
                {
                    TelephoneWhatsapp = wa;
                }
                OnPropertyChanged(nameof(EstChampWhatsappVisible));
                if (string.IsNullOrWhiteSpace(Telephone))
                    Erreur = "Aucun numéro enregistré, ajoute-le.";
                return;
            }
            Telephone = await SecureStorage.Default.GetAsync("auto237_telephone") ?? string.Empty;
        }
        catch
        {
            Telephone = await SecureStorage.Default.GetAsync("auto237_telephone") ?? string.Empty;
            Erreur = "Impossible de charger tes infos, vérifie ta connexion.";
        }
    }

    [RelayCommand]
    public async Task EnregistrerAsync()
    {
        Erreur = null;
        if (Chargement) return;
        if (string.IsNullOrWhiteSpace(Telephone))
        {
            Erreur = "Le téléphone est obligatoire.";
            return;
        }
        if (!EstMemeNumero && !string.IsNullOrWhiteSpace(TelephoneWhatsapp))
        {
            var chiffres = new string(TelephoneWhatsapp.Where(char.IsDigit).ToArray());
            if (chiffres.Length < 9)
            {
                Erreur = "Numéro WhatsApp invalide (9 chiffres minimum).";
                return;
            }
        }
        Chargement = true;
        try
        {
            var role = await SecureStorage.Default.GetAsync("auto237_role") ?? "client";
            bool estAgence = string.Equals(role, "agence", StringComparison.OrdinalIgnoreCase);
            var nom = await SecureStorage.Default.GetAsync("auto237_nom") ?? string.Empty;
            var nomAgence = estAgence ? await SecureStorage.Default.GetAsync("auto237_nomagence") : null;
            string? wa = EstMemeNumero ? Telephone.Trim() : (string.IsNullOrWhiteSpace(TelephoneWhatsapp) ? null : TelephoneWhatsapp.Trim());
            var (ok, err) = await _annonces.ModifierProfilAsync(Telephone.Trim(), wa, nom, nomAgence, estAgence);
            if (!ok) { Erreur = err; return; }
            await SecureStorage.Default.SetAsync("auto237_telephone", Telephone.Trim());
            if (!string.IsNullOrWhiteSpace(wa)) await SecureStorage.Default.SetAsync("auto237_telephone_whatsapp", wa);
            else SecureStorage.Default.Remove("auto237_telephone_whatsapp");
            await Shell.Current.DisplayAlertAsync("Modification", "Téléphone mis à jour.", "OK");
            await Shell.Current.GoToAsync("//profil");
        }
        catch (Exception ex) { Erreur = ex.Message; }
        finally { Chargement = false; }
    }
}
