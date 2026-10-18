using Auto237.Config;
using Auto237.Models;
using System.Net;

namespace Auto237.Services;

/// <summary>
/// Annonces / photos / favoris via Postgrest REST + Storage. Filtres serveur.
/// </summary>
public class AnnonceService
{
    public async Task<List<Vehicule>> ListerAsync(string? recherche = null, string? ville = null, string? boite = null, string? typeOffre = null, int page = 0, int taille = 20)
    {
        var svc = SupabaseService.Instance;
        var q = new List<string> { "select=*", "statut=eq.publie", "order=created_at.desc", $"limit={taille}", $"offset={page * taille}" };
        if (!string.IsNullOrWhiteSpace(ville) && ville != "Toutes")
            q.Add($"ville=eq.{WebUtility.UrlEncode(ville!)}");
        if (!string.IsNullOrWhiteSpace(boite) && boite != "Toutes")
            q.Add($"boite=eq.{boite!.ToLower()}");
        if (!string.IsNullOrWhiteSpace(typeOffre) && typeOffre != "Tous")
            q.Add($"type_offre=eq.{typeOffre!.ToLower()}");
        if (!string.IsNullOrWhiteSpace(recherche))
            q.Add($"or=(titre.ilike.*{WebUtility.UrlEncode(recherche!.Trim())}*,marque.ilike.*{WebUtility.UrlEncode(recherche!.Trim())}*,modele.ilike.*{WebUtility.UrlEncode(recherche!.Trim())}*,ville.ilike.*{WebUtility.UrlEncode(recherche!.Trim())}*)");

        var res = await svc.GetAsync($"{svc.Rest("vehicules")}?{string.Join("&", q)}");
        var liste = await svc.LisJsonAsync<List<Vehicule>>(res) ?? new();
        foreach (var v in liste) v.Photos = await PhotosDeAsync(v.Id);
        await EnrichirVendeursAsync(liste);

        var uid = await svc.UserIdAsync();
        if (!string.IsNullOrEmpty(uid))
        {
            try
            {
                var f = await svc.GetAsync($"{svc.Rest("favoris")}?select=vehicule_id&user_id=eq.{uid}");
                var favs = await svc.LisJsonAsync<List<Favori>>(f) ?? new();
                var set = favs.Select(x => x.VehiculeId).ToHashSet();
                foreach (var v in liste) v.EstFavori = set.Contains(v.Id);
            }
            catch { }
        }
        return liste;
    }

    public async Task<Vehicule?> DetailAsync(string id, bool incrementerVues = true)
    {
        var svc = SupabaseService.Instance;
        var res = await svc.GetAsync($"{svc.Rest("vehicules")}?select=*&id=eq.{id}&limit=1");
        var liste = await svc.LisJsonAsync<List<Vehicule>>(res) ?? new();
        var v = liste.FirstOrDefault();
        if (v == null) return null;
        v.Photos = await PhotosDeAsync(v.Id);
        await EnrichirVendeursAsync(new List<Vehicule> { v });
        var uid = await svc.UserIdAsync();
        if (!string.IsNullOrEmpty(uid))
        {
            try
            {
                var f = await svc.GetAsync($"{svc.Rest("favoris")}?select=vehicule_id&user_id=eq.{uid}&vehicule_id=eq.{id}");
                var favs = await svc.LisJsonAsync<List<Favori>>(f) ?? new();
                v.EstFavori = favs.Count > 0;
            }
            catch { }
        }
        if (incrementerVues && !string.IsNullOrEmpty(uid) && !string.Equals(uid, v.UserId, StringComparison.OrdinalIgnoreCase))
        {
            try { await svc.PatchJsonAsync($"{svc.Rest("vehicules")}?id=eq.{id}", new { vues = v.Vues + 1 }); v.Vues += 1; } catch { }
        }
        return v;
    }

    public async Task<List<Photo>> PhotosDeAsync(string vehiculeId)
    {
        var svc = SupabaseService.Instance;
        var res = await svc.GetAsync($"{svc.Rest("photos")}?select=*&vehicule_id=eq.{vehiculeId}&order=ordre.asc");
        return await svc.LisJsonAsync<List<Photo>>(res) ?? new();
    }

    public async Task<Vehicule> PublierAsync(Vehicule v, List<string> cheminsPhotosLocales)
    {
        var svc = SupabaseService.Instance;
        var uid = await svc.UserIdAsync() ?? throw new InvalidOperationException("Connecte-toi pour publier.");
        v.Id = Guid.NewGuid().ToString();
        v.UserId = uid;
        v.Statut = "publie";
        v.CreatedAt = DateTime.UtcNow;
        var res = await svc.PostJsonAsync(svc.Rest("vehicules"), v);
        var cree = (await svc.LisJsonAsync<List<Vehicule>>(res))?.First() ?? v;
        int ordre = 0;
        foreach (var local in cheminsPhotosLocales.Take(5))
        {
            var url = await UploaderPhotoAsync(cree.Id, local);
            await svc.PostJsonAsync(svc.Rest("photos"), new Photo { VehiculeId = cree.Id, Url = url, Ordre = ordre++ });
        }
        return cree;
    }

    private async Task<string> UploaderPhotoAsync(string vehiculeId, string cheminLocal)
    {
        var svc = SupabaseService.Instance;
        var ext = Path.GetExtension(cheminLocal);
        if (string.IsNullOrEmpty(ext)) ext = ".jpg";
        var nom = $"{vehiculeId}/{Guid.NewGuid():N}{ext}";
        var bytes = await File.ReadAllBytesAsync(cheminLocal);
        var ct = ext.ToLower() == ".png" ? "image/png" : "image/jpeg";
        await svc.UploaderAsync(nom, bytes, ct);
        return svc.PublicStorageUrl(nom);
    }

    public async Task BasculerFavoriAsync(string vehiculeId, bool ajouter)
    {
        var svc = SupabaseService.Instance;
        var uid = await svc.UserIdAsync() ?? throw new InvalidOperationException("Connecte-toi.");
        if (ajouter)
            await svc.PostJsonAsync(svc.Rest("favoris"), new Favori { UserId = uid, VehiculeId = vehiculeId });
        else
            await svc.DeleteAsync($"{svc.Rest("favoris")}?user_id=eq.{uid}&vehicule_id=eq.{vehiculeId}");
    }

    public async Task<List<Vehicule>> MesFavorisAsync()
    {
        var svc = SupabaseService.Instance;
        var uid = await svc.UserIdAsync() ?? throw new InvalidOperationException("Connecte-toi.");
        var f = await svc.GetAsync($"{svc.Rest("favoris")}?select=vehicule_id&user_id=eq.{uid}&order=created_at.desc");
        var favs = await svc.LisJsonAsync<List<Favori>>(f) ?? new();
        var result = new List<Vehicule>();
        foreach (var x in favs)
        {
            try
            {
                var v = await DetailAsync(x.VehiculeId, incrementerVues: false);
                if (v != null) { v.EstFavori = true; result.Add(v); }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Favori ignore {x.VehiculeId}: {ex.Message}");
            }
        }
        return result;
    }

    public async Task<List<Vehicule>> MesAnnoncesAsync()
    {
        var svc = SupabaseService.Instance;
        var uid = await svc.UserIdAsync() ?? throw new InvalidOperationException("Connecte-toi.");
        var res = await svc.GetAsync($"{svc.Rest("vehicules")}?select=*&user_id=eq.{uid}&order=created_at.desc");
        var liste = await svc.LisJsonAsync<List<Vehicule>>(res) ?? new();
        await EnrichirVendeursAsync(liste);
        foreach (var v in liste) v.Photos = await PhotosDeAsync(v.Id);
        return liste;
    }

    public async Task<int> CompterAnnoncesDuMoisAsync()
    {
        var svc = SupabaseService.Instance;
        var uid = await svc.UserIdAsync() ?? throw new InvalidOperationException("Connecte-toi.");
        var debutMois = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var iso = debutMois.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var res = await svc.GetAsync($"{svc.Rest("vehicules")}?select=id&user_id=eq.{uid}&created_at=gte.{iso}");
        var liste = await svc.LisJsonAsync<List<Vehicule>>(res) ?? new();
        return liste.Count;
    }

    public async Task SupprimerAnnonceAsync(string id)
    {
        var svc = SupabaseService.Instance;
        var photos = await PhotosDeAsync(id);
        foreach (var ph in photos) { try { await SupprimerFichierStorageParUrlAsync(ph.Url); } catch { } }
        var uid = await svc.UserIdAsync();
        if (string.IsNullOrEmpty(uid)) throw new InvalidOperationException("Connecte-toi pour supprimer.");
        try { await svc.DeleteAsync($"{svc.Rest("favoris")}?vehicule_id=eq.{id}"); } catch { }
        try { await svc.DeleteAsync($"{svc.Rest("photos")}?vehicule_id=eq.{id}"); } catch { }
        var res = await svc.DeleteAsync($"{svc.Rest("vehicules")}?id=eq.{id}&user_id=eq.{uid}");
        if (!res.IsSuccessStatusCode)
        {
            var txt = await res.Content.ReadAsStringAsync();
            throw new InvalidOperationException("Suppression refusee : " + txt);
        }
    }

    public async Task<Profil?> MonProfilAsync()
    {
        var svc = SupabaseService.Instance;
        var uid = await svc.UserIdAsync();
        if (string.IsNullOrEmpty(uid)) return null;
        var res = await svc.GetAsync($"{svc.Rest("profils")}?select=id,nom,telephone,telephone_whatsapp,nom_agence,role,photo_url&id=eq.{uid}&limit=1");
        var liste = await svc.LisJsonAsync<List<Profil>>(res) ?? new();
        return liste.FirstOrDefault();
    }

    public async Task PatchNomAgenceAsync(string nomAgence)
    {
        var svc = SupabaseService.Instance;
        var uid = await svc.UserIdAsync();
        if (string.IsNullOrEmpty(uid) || string.IsNullOrWhiteSpace(nomAgence)) return;
        try { await svc.PatchJsonAsync($"{svc.Rest("profils")}?id=eq.{uid}", new { nom_agence = nomAgence.Trim() }); } catch { }
    }

    public async Task<string?> TelephoneProfilAsync()
    {
        var svc = SupabaseService.Instance;
        var uid = await svc.UserIdAsync();
        if (string.IsNullOrEmpty(uid)) return null;
        var res = await svc.GetAsync($"{svc.Rest("profils")}?select=telephone&id=eq.{uid}&limit=1");
        var liste = await svc.LisJsonAsync<List<Profil>>(res) ?? new();
        return liste.FirstOrDefault()?.Telephone;
    }

    public async Task<(bool ok, string? erreur)> ModifierProfilAsync(string telephone, string? telephoneWhatsapp, string nom, string? nomAgence, bool estAgence)
    {
        var svc = SupabaseService.Instance;
        var uid = await svc.UserIdAsync();
        if (string.IsNullOrEmpty(uid)) return (false, "Connecte-toi.");
        string? wa = string.IsNullOrWhiteSpace(telephoneWhatsapp) ? null : telephoneWhatsapp.Trim();
        object payload = estAgence
            ? new
            {
                telephone = string.IsNullOrWhiteSpace(telephone) ? null : telephone.Trim(),
                telephone_whatsapp = wa,
                nom = string.IsNullOrWhiteSpace(nom) ? string.Empty : nom.Trim(),
                nom_agence = string.IsNullOrWhiteSpace(nomAgence) ? null : nomAgence.Trim()
            }
            : new
            {
                telephone = string.IsNullOrWhiteSpace(telephone) ? null : telephone.Trim(),
                telephone_whatsapp = wa,
                nom = string.IsNullOrWhiteSpace(nom) ? string.Empty : nom.Trim()
            };
        var res = await svc.PatchJsonAsync($"{svc.Rest("profils")}?id=eq.{uid}", payload);
        if (!res.IsSuccessStatusCode)
        {
            var txt = await res.Content.ReadAsStringAsync();
            if (txt.Contains("PGRST204", StringComparison.OrdinalIgnoreCase))
                return (false, "Champ inconnu, mets à jour l'application.");
            if (txt.Contains("nom_agence", StringComparison.OrdinalIgnoreCase) || txt.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
                return (false, "Ce nom d'agence existe déjà.");
            return (false, $"Modification refusée : {txt}");
        }
        return (true, null);
    }

    /// <summary>
    /// Enrichit les annonces avec role + nom agence + photo du vendeur (1 seul GET profils groupe).
    /// </summary>
    public async Task EnrichirVendeursAsync(List<Vehicule> liste)
    {
        if (liste.Count == 0) return;
        try
        {
            var svc = SupabaseService.Instance;
            var ids = liste.Where(v => !string.IsNullOrWhiteSpace(v.UserId)).Select(v => v.UserId).Distinct().ToList();
            if (ids.Count == 0) return;
            var res = await svc.GetAsync($"{svc.Rest("profils")}?select=id,role,nom,nom_agence,photo_url&id=in.({string.Join(",", ids)})");
            var profils = await svc.LisJsonAsync<List<Profil>>(res) ?? new();
            var map = profils.ToDictionary(p => p.Id, p => p);
            foreach (var v in liste)
            {
                if (map.TryGetValue(v.UserId, out var p))
                {
                    v.RoleVendeur = string.IsNullOrWhiteSpace(p.Role) ? "client" : p.Role;
                    v.NomVendeur = p.Nom;
                    v.NomAgenceVendeur = p.NomAgence;
                    v.PhotoVendeur = p.PhotoUrl;
                }
            }
        }
        catch { }
    }

    /// <summary>
    /// Choisit une photo de profil (galerie), l'upload vers profils/{uid}.jpg et met a jour profils.photo_url.
    /// </summary>
    public async Task<string?> ChangerPhotoProfilAsync()
    {
        var svc = SupabaseService.Instance;
        var uid = await svc.UserIdAsync();
        if (string.IsNullOrEmpty(uid)) throw new InvalidOperationException("Connecte-toi pour changer ta photo.");
        var pick = await MediaPicker.Default.PickPhotoAsync();
        if (pick == null) return null;
        using var stream = await pick.OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var bytes = ms.ToArray();
        if (bytes.Length == 0) throw new InvalidOperationException("Image vide.");
        var chemin = $"profils/{uid}.jpg";
        await svc.UploaderAsync(chemin, bytes, "image/jpeg");
        var url = svc.PublicStorageUrl(chemin);
        var res = await svc.PatchJsonAsync($"{svc.Rest("profils")}?id=eq.{uid}", new { photo_url = url });
        if (!res.IsSuccessStatusCode)
        {
            var txt = await res.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Photo non enregistree : {txt}");
        }
        try { await SecureStorage.Default.SetAsync("auto237_photo", url); } catch { }
        return url;
    }
    public async Task<Vehicule> ModifierAnnonceAsync(string id, Vehicule v)
    {
        var svc = SupabaseService.Instance;
        var res = await svc.PatchJsonAsync($"{svc.Rest("vehicules")}?id=eq.{id}", new
        {
            titre = v.Titre, marque = v.Marque, modele = v.Modele, annee = v.Annee, ville = v.Ville,
            boite = v.Boite, type_offre = v.TypeOffre, prix_vente = v.PrixVente, prix_jour = v.PrixJour,
            description = v.Description, tel_contact = v.TelContact, tel_whatsapp = v.TelWhatsapp
        });
        if (!res.IsSuccessStatusCode)
        {
            var txt = await res.Content.ReadAsStringAsync();
            throw new InvalidOperationException("Modification refusee : " + txt);
        }
        v.Id = id;
        return v;
    }

    public async Task ChangerStatutAsync(string id, string statut)
    {
        var s = (statut ?? string.Empty).Trim().ToLower();
        if (s != "publie" && s != "vendu" && s != "loue")
            throw new InvalidOperationException("Statut invalide (publie, vendu ou loue).");
        var svc = SupabaseService.Instance;
        var res = await svc.PatchJsonAsync($"{svc.Rest("vehicules")}?id=eq.{id}", new { statut = s });
        if (!res.IsSuccessStatusCode)
        {
            var txt = await res.Content.ReadAsStringAsync();
            throw new InvalidOperationException("Changement de statut refuse : " + txt);
        }
    }

    public async Task SupprimerPhotoParUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        var svc = SupabaseService.Instance;
        try
        {
            var res = await svc.GetAsync($"{svc.Rest("photos")}?select=id&url=eq.{WebUtility.UrlEncode(url.Trim())}&limit=1");
            var lignes = await svc.LisJsonAsync<List<Photo>>(res) ?? new();
            var pid = lignes.FirstOrDefault()?.Id;
            if (!string.IsNullOrEmpty(pid))
            {
                try { await svc.DeleteAsync($"{svc.Rest("photos")}?id=eq.{pid}"); } catch { }
            }
            await SupprimerFichierStorageParUrlAsync(url);
        }
        catch { }
    }

    public async Task AjouterPhotoAsync(string vehiculeId, string cheminLocal)
    {
        if (string.IsNullOrWhiteSpace(vehiculeId) || string.IsNullOrWhiteSpace(cheminLocal)) return;
        var bytes = await File.ReadAllBytesAsync(cheminLocal);
        if (bytes.Length == 0 || bytes.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("Photo invalide (vide ou plus de 5 Mo).");
        var chemin = vehiculeId + "/" + Guid.NewGuid().ToString("N") + ".jpg";
        var svc = SupabaseService.Instance;
        await svc.UploaderAsync(chemin, bytes, "image/jpeg");
        var url = svc.PublicStorageUrl(chemin);
        var exist = await PhotosDeAsync(vehiculeId);
        var res = await svc.PostJsonAsync(svc.Rest("photos"), new { vehicule_id = vehiculeId, url, ordre = exist.Count });
        if (!res.IsSuccessStatusCode)
        {
            var txt = await res.Content.ReadAsStringAsync();
            throw new InvalidOperationException("Photo non enregistree : " + txt);
        }
    }

    public async Task SupprimerFichierStorageParUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            var svc = SupabaseService.Instance;
            var marqueur = "/" + SupabaseConfig.AnnoncesBucket + "/";
            var i = url.IndexOf(marqueur, StringComparison.OrdinalIgnoreCase);
            if (i < 0) return;
            var chemin = url.Substring(i + marqueur.Length);
            var del = SupabaseConfig.Url.TrimEnd('/') + "/storage/v1/object/" + SupabaseConfig.AnnoncesBucket + "/" + chemin;
            await svc.DeleteAsync(del);
        }
        catch { }
    }
}
