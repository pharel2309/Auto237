using Auto237.Models;
using System.Text.Json.Serialization;

namespace Auto237.Services;

/// <summary>
/// Auth réelle via Supabase Auth REST. Session = access_token + user id en SecureStorage.
/// </summary>
public class AuthService
{
    private const string KEY_USERID = "auto237_userid";
    private const string KEY_EMAIL = "auto237_email";
    private const string KEY_TOKEN = "auto237_token";
    private const string KEY_REFRESH = "auto237_refresh";
    private const string KEY_ROLE = "auto237_role";

    public sealed class SessionExpireeException : Exception
    {
        public SessionExpireeException() : base("Session expirée, reconnecte-toi.") { }
    }

    private sealed class AuthResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
        [JsonPropertyName("expires_in")] public int? ExpiresIn { get; set; }
        [JsonPropertyName("user")] public AuthUser? User { get; set; }
    }
    private sealed class AuthUser
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
    }

    public async Task<string?> UserIdAsync()
        => await SecureStorage.Default.GetAsync(KEY_USERID);

    public async Task<bool> EstConnecteAsync()
        => !string.IsNullOrEmpty(await UserIdAsync()) && !string.IsNullOrEmpty(await SecureStorage.Default.GetAsync(KEY_TOKEN));

    public async Task<(bool ok, string? erreur)> InscrireAsync(string nom, string email, string password, string telephone, string role, string? nomAgence = null)
    {
        try
        {
            var svc = SupabaseService.Instance;
            var res = await svc.PostJsonAsync(svc.AuthUrl("signup"), new { email = email.Trim(), password }, auth: false);
            var data = await svc.LisJsonAsync<AuthResponse>(res);
            var user = data?.User;
            if (user?.Id == null) return (false, "Inscription impossible. Vérifie l'email.");
            if (!string.IsNullOrEmpty(data?.AccessToken))
                try { await SecureStorage.Default.SetAsync(KEY_TOKEN, data.AccessToken); } catch { }
            if (!string.IsNullOrEmpty(data?.RefreshToken))
                try { await SecureStorage.Default.SetAsync(KEY_REFRESH, data.RefreshToken); } catch { }

            var profilPayload = new Profil
            {
                Id = user.Id,
                Nom = nom.Trim(),
                Telephone = string.IsNullOrWhiteSpace(telephone) ? null : telephone.Trim(),
                Role = role,
                NomAgence = string.Equals(role, "agence", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(nomAgence) ? nomAgence.Trim() : null
            };
            var profilRes = await svc.PostJsonAsync(svc.Rest("profils"), profilPayload);
            if (!profilRes.IsSuccessStatusCode)
            {
                var profilTxt = await profilRes.Content.ReadAsStringAsync();
                try
                {
                    var verif = await svc.GetAsync($"{svc.Rest("profils")}?select=id&id=eq.{user.Id}&limit=1");
                    var existant = await svc.LisJsonAsync<List<Profil>>(verif) ?? new();
                    if (existant.Count == 0)
                    {
                        if (profilTxt.Contains("row-level security", StringComparison.OrdinalIgnoreCase) || profilTxt.Contains("policy", StringComparison.OrdinalIgnoreCase) || profilTxt.Contains("permission denied", StringComparison.OrdinalIgnoreCase))
                            return (false, "Création du profil refusée par les règles de sécurité, contacte l'admin. Détail : " + profilTxt);
                        return (false, "Profil non créé : " + profilTxt);
                    }
                }
                catch { return (false, "Profil non créé : " + profilTxt); }
            }

            try { await SecureStorage.Default.SetAsync(KEY_USERID, user.Id); } catch { }
            try { await SecureStorage.Default.SetAsync(KEY_EMAIL, email.Trim()); } catch { }
            try { await SecureStorage.Default.SetAsync(KEY_ROLE, role); } catch { }
            try { await SecureStorage.Default.SetAsync("auto237_nom", nom.Trim()); } catch { }
            bool estAgence = string.Equals(role, "agence", StringComparison.OrdinalIgnoreCase);
            try { await SecureStorage.Default.SetAsync("auto237_nomagence", estAgence && !string.IsNullOrWhiteSpace(nomAgence) ? nomAgence.Trim() : null); } catch { }
            try { await SecureStorage.Default.SetAsync("auto237_responsable", nom.Trim()); } catch { }
            return (true, null);
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool ok, string? erreur)> ConnecterAsync(string email, string password)
    {
        try
        {
            var svc = SupabaseService.Instance;
            var res = await svc.PostJsonAsync(svc.AuthUrl("token?grant_type=password"), new { email = email.Trim(), password }, auth: false);
            var data = await svc.LisJsonAsync<AuthResponse>(res);
            if (data?.User?.Id == null || string.IsNullOrEmpty(data.AccessToken))
                return (false, "Email ou mot de passe incorrect.");
            try { await SecureStorage.Default.SetAsync(KEY_TOKEN, data.AccessToken); } catch { }
            if (!string.IsNullOrEmpty(data?.RefreshToken))
                try { await SecureStorage.Default.SetAsync(KEY_REFRESH, data.RefreshToken); } catch { }
            try { await SecureStorage.Default.SetAsync(KEY_USERID, data.User.Id); } catch { }
            await SecureStorage.Default.SetAsync(KEY_EMAIL, email.Trim());
            try
            {
                var p = await svc.GetAsync($"{svc.Rest("profils")}?select=role&id=eq.{data.User.Id}&limit=1", useRefresh: false);
                var liste = await svc.LisJsonAsync<List<Profil>>(p) ?? new();
                await SecureStorage.Default.SetAsync(KEY_ROLE, liste.FirstOrDefault()?.Role ?? "client");
            }
            catch { await SecureStorage.Default.SetAsync(KEY_ROLE, "client"); }
            try
            {
                var p2 = await svc.GetAsync($"{svc.Rest("profils")}?select=nom,telephone,nom_agence&id=eq.{data.User.Id}&limit=1", useRefresh: false);
                var liste2 = await svc.LisJsonAsync<List<Profil>>(p2) ?? new();
                var prof = liste2.FirstOrDefault();
                if (prof != null)
                {
                    await SecureStorage.Default.SetAsync("auto237_nom", prof.Nom ?? string.Empty);
                    await SecureStorage.Default.SetAsync("auto237_telephone", prof.Telephone ?? string.Empty);
                    await SecureStorage.Default.SetAsync("auto237_nomagence", prof.NomAgence ?? string.Empty);
                    await SecureStorage.Default.SetAsync("auto237_responsable", prof.Nom ?? string.Empty);
                }
            }
            catch { }
            return (true, null);
        }
        catch (Exception ex) { return (false, "Email ou mot de passe incorrect. " + ex.Message); }
    }

    public async Task EnvoyerLienResetAsync(string email)
    {
        var svc = SupabaseService.Instance;
        var res = await svc.PostJsonAsync(svc.AuthUrl("recover"), new { email = email.Trim() }, auth: false, preferReturn: false);
        var txt = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException($"Reset {(int)res.StatusCode} : {txt}");
    }

    public async Task<(bool ok, string? erreur)> ChangePasswordAsync(string ancien, string nouveau)
    {
        try
        {
            var svc = SupabaseService.Instance;
            if (!svc.EstConfigure) return (false, "Configuration Supabase manquante.");
            var res = await svc.PatchJsonAsync(svc.AuthUrl("user"), new
            {
                data = new { password = nouveau, password_confirm = nouveau }
            });
            if (!res.IsSuccessStatusCode)
            {
                var txt = await res.Content.ReadAsStringAsync();
                return (false, $"Mot de passe refusé : {txt}");
            }
            return (true, null);
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<bool> RefreshSessionAsync()
    {
        try
        {
            var svc = SupabaseService.Instance;
            if (!svc.EstConfigure) return false;
            var refresh = await SecureStorage.Default.GetAsync(KEY_REFRESH);
            if (string.IsNullOrEmpty(refresh)) return false;
            var res = await svc.PostJsonAsync(svc.AuthUrl("token?grant_type=refresh_token"), new { refresh_token = refresh }, auth: false, preferReturn: false);
            var txt = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode) return false;
            var data = await svc.LisJsonAsync<AuthResponse>(res);
            if (string.IsNullOrEmpty(data?.AccessToken)) return false;
            try { await SecureStorage.Default.SetAsync(KEY_TOKEN, data.AccessToken); } catch { }
            if (!string.IsNullOrEmpty(data?.RefreshToken))
                try { await SecureStorage.Default.SetAsync(KEY_REFRESH, data.RefreshToken); } catch { }
            if (!string.IsNullOrEmpty(data?.User?.Id))
                try { await SecureStorage.Default.SetAsync(KEY_USERID, data.User.Id); } catch { }
            return true;
        }
        catch { return false; }
    }

    public async Task DeconnecterAsync()
    {
        try
        {
            var svc = SupabaseService.Instance;
            if (svc.EstConfigure)
            {
                try { await svc.PostJsonAsync(svc.AuthUrl("logout"), new { }, preferReturn: false); } catch { }
            }
        }
        catch { }
        SecureStorage.Default.Remove(KEY_USERID);
        SecureStorage.Default.Remove(KEY_EMAIL);
        SecureStorage.Default.Remove(KEY_TOKEN);
        SecureStorage.Default.Remove(KEY_REFRESH);
        SecureStorage.Default.Remove(KEY_ROLE);
    }
}

