using Auto237.Config;
using Auto237.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Auto237.Services;

/// <summary>
/// Client REST Supabase natif (Auth + Postgrest + Storage) sans package externe.
/// Utilise Project URL + AnonKey. Ne JAMAIS utiliser les tokens sbp_ ici.
/// </summary>
public sealed class SupabaseService
{
    private static readonly Lazy<SupabaseService> _instance = new(() => new SupabaseService());
    public static SupabaseService Instance => _instance.Value;

    private readonly HttpClient _http = new();
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public bool EstConfigure => SupabaseConfig.IsConfigured;

    private void AppliqueHeaders(bool auth = false, string? token = null)
    {
        _http.DefaultRequestHeaders.Remove("apikey");
        _http.DefaultRequestHeaders.Remove("Authorization");
        _http.DefaultRequestHeaders.Add("apikey", SupabaseConfig.AnonKey);
        if (auth && !string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public string Rest(string table) => $"{SupabaseConfig.Url.TrimEnd('/')}/rest/v1/{table}";
    public string AuthUrl(string path) => $"{SupabaseConfig.Url.TrimEnd('/')}/auth/v1/{path}";

    public async Task<string?> TokenAsync()
        => await SecureStorage.Default.GetAsync("auto237_token");

    public async Task<string?> UserIdAsync()
        => await SecureStorage.Default.GetAsync("auto237_userid");

    public async Task<HttpResponseMessage> GetAsync(string url, bool auth = true, bool useRefresh = true)
    {
        if (!EstConfigure) throw new InvalidOperationException("Supabase non configuré.");
        AppliqueHeaders(auth, await TokenAsync());
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var res = await _http.SendAsync(req);
        if (useRefresh && auth && (int)res.StatusCode == 401 && await EstJwtExpireAsync(res))
        {
            var authSvc = new AuthService();
            if (await authSvc.RefreshSessionAsync())
            {
                AppliqueHeaders(auth, await TokenAsync());
                var retry = new HttpRequestMessage(HttpMethod.Get, url);
                retry.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                res = await _http.SendAsync(retry);
                if ((int)res.StatusCode == 401) throw new AuthService.SessionExpireeException();
                return res;
            }
            throw new AuthService.SessionExpireeException();
        }
        return res;
    }

    private static async Task<bool> EstJwtExpireAsync(HttpResponseMessage res)
    {
        try
        {
            var txt = await res.Content.ReadAsStringAsync();
            return txt.Contains("PGRST303", StringComparison.OrdinalIgnoreCase) || txt.Contains("JWT expired", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    public async Task<HttpResponseMessage> PostJsonAsync(string url, object body, bool auth = true, bool preferReturn = true, bool useRefresh = true)
    {
        if (!EstConfigure) throw new InvalidOperationException("Supabase non configuré.");
        AppliqueHeaders(auth, await TokenAsync());
        var json = JsonSerializer.Serialize(body, JsonOpts);
        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (preferReturn) req.Headers.TryAddWithoutValidation("Prefer", "return=representation");
        var res = await _http.SendAsync(req);
        if (useRefresh && auth && (int)res.StatusCode == 401 && await EstJwtExpireAsync(res))
        {
            var authSvc = new AuthService();
            if (await authSvc.RefreshSessionAsync())
            {
                AppliqueHeaders(auth, await TokenAsync());
                var retry = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                retry.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (preferReturn) retry.Headers.TryAddWithoutValidation("Prefer", "return=representation");
                res = await _http.SendAsync(retry);
                if ((int)res.StatusCode == 401) throw new AuthService.SessionExpireeException();
                return res;
            }
            throw new AuthService.SessionExpireeException();
        }
        return res;
    }

    public async Task<HttpResponseMessage> PatchJsonAsync(string url, object body, bool useRefresh = true)
    {
        if (!EstConfigure) throw new InvalidOperationException("Supabase non configuré.");
        AppliqueHeaders(true, await TokenAsync());
        var json = JsonSerializer.Serialize(body, JsonOpts);
        var req = new HttpRequestMessage(new HttpMethod("PATCH"), url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        req.Headers.TryAddWithoutValidation("Prefer", "return=representation");
        var res = await _http.SendAsync(req);
        if (useRefresh && (int)res.StatusCode == 401 && await EstJwtExpireAsync(res))
        {
            var authSvc = new AuthService();
            if (await authSvc.RefreshSessionAsync())
            {
                AppliqueHeaders(true, await TokenAsync());
                var retry = new HttpRequestMessage(new HttpMethod("PATCH"), url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                retry.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                retry.Headers.TryAddWithoutValidation("Prefer", "return=representation");
                res = await _http.SendAsync(retry);
                if ((int)res.StatusCode == 401) throw new AuthService.SessionExpireeException();
                return res;
            }
            throw new AuthService.SessionExpireeException();
        }
        return res;
    }

    public async Task<HttpResponseMessage> DeleteAsync(string url, bool useRefresh = true)
    {
        if (!EstConfigure) throw new InvalidOperationException("Supabase non configuré.");
        AppliqueHeaders(true, await TokenAsync());
        var res = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Delete, url));
        if (useRefresh && (int)res.StatusCode == 401 && await EstJwtExpireAsync(res))
        {
            var authSvc = new AuthService();
            if (await authSvc.RefreshSessionAsync())
            {
                AppliqueHeaders(true, await TokenAsync());
                res = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Delete, url));
                if ((int)res.StatusCode == 401) throw new AuthService.SessionExpireeException();
                return res;
            }
            throw new AuthService.SessionExpireeException();
        }
        return res;
    }

    public async Task<T?> LisJsonAsync<T>(HttpResponseMessage res)
    {
        var txt = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException($"Supabase {(int)res.StatusCode} : {txt}");
        if (string.IsNullOrWhiteSpace(txt)) return default;
        return JsonSerializer.Deserialize<T>(txt, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public string PublicStorageUrl(string chemin)
        => $"{SupabaseConfig.Url.TrimEnd('/')}/storage/v1/object/public/{SupabaseConfig.AnnoncesBucket}/{chemin}";

    public async Task UploaderAsync(string chemin, byte[] bytes, string contentType = "image/jpeg")
    {
        if (!EstConfigure) throw new InvalidOperationException("Supabase non configuré.");
        AppliqueHeaders(true, await TokenAsync());
        var url = $"{SupabaseConfig.Url.TrimEnd('/')}/storage/v1/object/{SupabaseConfig.AnnoncesBucket}/{chemin}";
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var res = await _http.PostAsync(url, content);
        if ((int)res.StatusCode == 401)
        {
            var authSvc = new AuthService();
            if (await authSvc.RefreshSessionAsync())
            {
                AppliqueHeaders(true, await TokenAsync());
                content = new ByteArrayContent(bytes);
                content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                res = await _http.PostAsync(url, content);
                if ((int)res.StatusCode == 401) throw new AuthService.SessionExpireeException();
            }
            else throw new AuthService.SessionExpireeException();
        }
        var txt = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException($"Upload {(int)res.StatusCode} : {txt}");
    }
}

