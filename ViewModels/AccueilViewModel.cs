using Auto237.Models;
using Auto237.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Auto237.ViewModels;

public partial class AccueilViewModel : ObservableObject
{
    private readonly AnnonceService _annonces = new();
    private readonly Services.AuthService _auth = new();

    private bool _enCours;

    [ObservableProperty] string recherche = string.Empty;
    [ObservableProperty] string ville = "Toutes";
    [ObservableProperty] string boite = "Toutes";
    [ObservableProperty] string typeOffre = "Tous";
    [ObservableProperty] bool chargement;
    [ObservableProperty] string? messageErreur;
    [ObservableProperty] bool supabaseManquant;
    [ObservableProperty] bool estErreur;
    [ObservableProperty] bool sessionExpiree;
    [ObservableProperty] string messageVide = "Aucune annonce pour le moment.";

    public bool EstVide => Annonces.Count == 0 && !Chargement && !EstErreur;

    public ObservableCollection<Vehicule> Annonces { get; } = new();
    public List<string> Villes { get; } = new() { "Toutes", "Douala", "Yaoundé", "Bafoussam", "Kribi", "Garoua", "Bamenda" };
    public List<string> Boites { get; } = new() { "Toutes", "Manuelle", "Auto" };
    public List<string> Types { get; } = new() { "Tous", "Vente", "Location" };

    [RelayCommand]
    public async Task ChargerAsync()
    {
        if (_enCours) return;
        _enCours = true;
        if (!Config.SupabaseConfig.IsConfigured)
        {
            SupabaseManquant = true;
            MessageErreur = "Supabase non configuré. Renseigne URL + anon key dans Config/SupabaseConfig.cs puis crée les tables (supabase/01_tables.sql + 02_rls_storage.sql).";
            _enCours = false;
            return;
        }
        SupabaseManquant = false;
        try
        {
            Chargement = true;
            EstErreur = false;
            MessageErreur = null;
            var liste = await _annonces.ListerAsync(Recherche, Ville, Boite, TypeOffre);
            Annonces.Clear();
            var vus = new HashSet<string>();
            foreach (var v in liste)
            {
                if (vus.Add(v.Id)) Annonces.Add(v);
            }
            OnPropertyChanged(nameof(EstVide));
        }
        catch (Exception ex)
        {
            EstErreur = true;
            MessageErreur = Services.ErreursSupabase.MessageAimable(ex.Message);
            SessionExpiree = ex is Services.AuthService.SessionExpireeException || (MessageErreur?.Contains("Session expirée") == true);
        }
        finally
        {
            Chargement = false;
            OnPropertyChanged(nameof(EstVide));
            _enCours = false;
        }
    }

    [RelayCommand]
    public async Task SeReconnecterAsync()
    {
        try { await _auth.DeconnecterAsync(); } catch { }
        try { await Shell.Current.GoToAsync("//connexion"); } catch { }
    }

    [RelayCommand]
    public async Task OuvrirDetailAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        try { await Shell.Current.GoToAsync($"detail?id={id}"); }
        catch (Exception ex) { await Shell.Current.DisplayAlertAsync("Navigation", ex.Message, "OK"); }
    }

    [RelayCommand]
    public async Task BasculerFavoriAsync(Vehicule v)
    {
        if (Chargement) return;
        if (v == null) return;
        try
        {
            await _annonces.BasculerFavoriAsync(v.Id, !v.EstFavori);
            v.EstFavori = !v.EstFavori;
            var idx = Annonces.IndexOf(v);
            if (idx >= 0)
            {
                Annonces.RemoveAt(idx);
                Annonces.Insert(idx, v);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Favoris", ex.Message, "OK");
        }
    }
}
