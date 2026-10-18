using Auto237.Models;
using Auto237.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Auto237.ViewModels;

public partial class MesAnnoncesViewModel : ObservableObject
{
    private readonly AnnonceService _annonces = new();
    private bool _enCours;
    [ObservableProperty] bool chargement;
    [ObservableProperty] string? message;
    public ObservableCollection<Vehicule> Annonces { get; } = new();

    [RelayCommand]
    public async Task ChargerAsync()
    {
        if (_enCours) return;
        _enCours = true;
        try
        {
            Chargement = true; Message = null;
            var liste = await _annonces.MesAnnoncesAsync();
            Annonces.Clear();
            var vus = new HashSet<string>();
            foreach (var v in liste)
            {
                if (vus.Add(v.Id)) Annonces.Add(v);
            }
            if (Annonces.Count == 0) Message = "Tu n'as pas encore publié d'annonce.";
        }
        catch (Exception ex) { Message = ex.Message; }
        finally { Chargement = false; _enCours = false; }
    }

    [RelayCommand]
    public async Task RetourAsync()
    {
        try { await Shell.Current.GoToAsync("//profil"); }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Navigation", ex.Message, "OK"); }
    }

    [RelayCommand]
    public async Task ModifierAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        try { await Shell.Current.GoToAsync($"//publier-form?id={id}"); }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Navigation", ex.Message, "OK"); }
    }

    [RelayCommand]
    public async Task ChangerStatutAsync(Vehicule v)
    {
        if (v == null || Chargement) return;
        try
        {
            Chargement = true;
            var estVendu = string.Equals(v.Statut, "vendu", StringComparison.OrdinalIgnoreCase)
                || string.Equals(v.Statut, "loue", StringComparison.OrdinalIgnoreCase);
            if (estVendu)
            {
                bool ok = await Shell.Current.DisplayAlertAsync("Remettre en ligne", $"Remettre '{v.Titre}' en ligne ?", "Oui", "Non");
                if (!ok) return;
                await _annonces.ChangerStatutAsync(v.Id, "publie");
                v.Statut = "publie";
            }
            else
            {
                var cible = string.Equals(v.TypeOffre, "location", StringComparison.OrdinalIgnoreCase) ? "loue" : "vendu";
                var libelle = cible == "loue" ? "loue" : "vendu";
                bool ok = await Shell.Current.DisplayAlertAsync("Statut", $"Marquer '{v.Titre}' comme {libelle} ?", "Oui", "Non");
                if (!ok) return;
                await _annonces.ChangerStatutAsync(v.Id, cible);
                v.Statut = cible;
            }
            var idx = Annonces.IndexOf(v);
            if (idx >= 0) { Annonces.RemoveAt(idx); Annonces.Insert(idx, v); }
        }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Statut", ex.Message, "OK"); }
        finally { Chargement = false; }
    }

    [RelayCommand]
    public async Task SupprimerAsync(Vehicule v)
    {
        if (v == null) return;
        try
        {
            bool ok = await Shell.Current.DisplayAlertAsync("Supprimer", $"Supprimer '{v.Titre}' ?", "Oui", "Non");
            if (!ok) return;
            await _annonces.SupprimerAnnonceAsync(v.Id);
            Annonces.Remove(v);
        }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Supprimer", ex.Message, "OK"); }
    }

    [RelayCommand]
    public async Task OuvrirDetailAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        try { await Shell.Current.GoToAsync($"detail?id={id}"); }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Navigation", ex.Message, "OK"); }
    }
}
