using Auto237.Models;
using Auto237.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Auto237.ViewModels;

public partial class FavorisViewModel : ObservableObject
{
    private readonly AnnonceService _annonces = new();
    private readonly Services.AuthService _auth = new();
    private bool _enCours;
    [ObservableProperty] bool chargement;
    [ObservableProperty] string? message;
    [ObservableProperty] bool estErreur;
    [ObservableProperty] bool sessionExpiree;
    [ObservableProperty] string messageVide = "Aucun favori pour le moment.";
    public ObservableCollection<Vehicule> Favoris { get; } = new();

    public bool EstVide => Favoris.Count == 0 && !Chargement && !EstErreur;

    [RelayCommand]
    public async Task ChargerAsync()
    {
        if (_enCours) return;
        _enCours = true;
        try
        {
            Chargement = true; Message = null; EstErreur = false;
            OnPropertyChanged(nameof(EstVide));
            var liste = await _annonces.MesFavorisAsync();
            Favoris.Clear();
            var vus = new HashSet<string>();
            foreach (var v in liste)
            {
                if (vus.Add(v.Id)) Favoris.Add(v);
            }
        }
        catch (Exception ex)
        {
            EstErreur = true;
            Message = Services.ErreursSupabase.MessageAimable(ex.Message);
            SessionExpiree = ex is Services.AuthService.SessionExpireeException || (Message?.Contains("Session expirée") == true);
        }
        finally { Chargement = false; OnPropertyChanged(nameof(EstVide)); _enCours = false; }
    }

    [RelayCommand]
    public async Task OuvrirDetailAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        try { await Shell.Current.GoToAsync($"detail?id={id}"); }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Navigation", ex.Message, "OK"); }
    }

    [RelayCommand]
    public async Task SeReconnecterAsync()
    {
        try { await _auth.DeconnecterAsync(); } catch { }
        try { await Shell.Current.GoToAsync("//connexion"); } catch { }
    }

    [RelayCommand]
    public async Task RetirerAsync(Vehicule v)
    {
        if (v == null) return;
        try
        {
            await _annonces.BasculerFavoriAsync(v.Id, false);
            Favoris.Remove(v);
        }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Favoris", ex.Message, "OK"); }
    }

    [RelayCommand]
    public async Task BasculerFavoriAsync(Vehicule v)
    {
        if (v == null) return;
        try
        {
            var nouvelEtat = !v.EstFavori;
            await _annonces.BasculerFavoriAsync(v.Id, nouvelEtat);
            v.EstFavori = nouvelEtat;
            if (!nouvelEtat) Favoris.Remove(v);
            else
            {
                var idx = Favoris.IndexOf(v);
                if (idx >= 0)
                {
                    Favoris.RemoveAt(idx);
                    Favoris.Insert(idx, v);
                }
            }
        }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Favoris", ex.Message, "OK"); }
    }
}
