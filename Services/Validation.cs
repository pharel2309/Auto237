using System.Text.RegularExpressions;

namespace Auto237.Services;

public static class Validation
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static bool EmailValide(string? email)
        => !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(NormaliserEmail(email));

    /// <summary>
    /// Emails acceptés par la plateforme : pas de point dans la partie locale
    /// (le service d'envoi Supabase les rejette en email_address_invalid).
    /// Ex. particuliertest@gmail.com OK, particulier.test@gmail.com refusé.
    /// </summary>
    public static bool EmailAccepte(string? email)
    {
        var n = NormaliserEmail(email);
        if (!EmailValide(n)) return false;
        var arobase = n.IndexOf('@');
        if (arobase <= 0) return false;
        return !n.Substring(0, arobase).Contains('.');
    }

    /// <summary>
    /// Normalise un email saisi : retire espaces, espaces insécables et
    /// caractères invisibles (copier-coller, suggestions clavier), passe en minuscule.
    /// </summary>
    public static string NormaliserEmail(string? email)
    {
        if (string.IsNullOrEmpty(email)) return string.Empty;
        var n = email.Trim().ToLowerInvariant();
        n = n.Replace("\u00A0", string.Empty).Replace("\u200B", string.Empty)
             .Replace("\u200C", string.Empty).Replace("\u200D", string.Empty)
             .Replace("\uFEFF", string.Empty).Replace(" ", string.Empty);
        return n;
    }
}

public static class ErreursSupabase
{
    public static string MessageAimable(string brut)
    {
        if (string.IsNullOrWhiteSpace(brut)) return "Une erreur est survenue. Réessaie.";
        var b = brut.ToLowerInvariant();
        if (b.Contains("invalid_credentials") || b.Contains("invalid login credentials"))
            return "Email ou mot de passe incorrect.";
        if (b.Contains("email_not_confirmed"))
            return "Compte créé, vérifie ta boîte mail pour confirmer ton email (ou désactive Confirm email en démo).";
        if (b.Contains("over_email_send_rate_limit") || b.Contains("rate limit") || b.Contains("429"))
            return "Trop d'emails envoyés, patiente 10 min avant de réessayer.";
        if (b.Contains("user_already_exists") || b.Contains("already registered") || b.Contains("duplicate"))
            return "Un compte existe déjà avec cet email, connecte-toi.";
        if (b.Contains("weak_password"))
            return "Choisis un mot de passe plus robuste (6 caractères minimum).";
        if (b.Contains("pgrst303") || b.Contains("jwt expired") || b.Contains("session expirée") || b.Contains("sessionexpireeexception"))
            return "Session expirée, reconnecte-toi.";
        if (b.Contains("row-level security") || b.Contains("policy") || b.Contains("permission denied"))
            return "Création du profil refusée par les règles de sécurité, contacte l'admin.";
        if (b.Contains("profil non créé"))
            return brut;
        if (b.Contains("email_address_invalid"))
            return "Ce service d'email refuse cette adresse pour l'instant (adresse rejetée côté serveur). Réessaie avec une adresse sans point ou contacte l'agence.";
        if (b.Contains("invalid") && b.Contains("email"))
            return "Cet email semble invalide, vérifie le @ et le .com — ex. admin@gmail.com";
        if (b.Contains("too many requests") || b.Contains("email rate limit") || b.Contains("email_rate_limit") || b.Contains("exceeded"))
            return "Trop d'emails envoyés, patiente 10 min avant de réessayer.";
        if (b.Contains("user not found") || b.Contains("email not found") || b.Contains("not found") && b.Contains("user"))
            return "Aucun compte avec cet email. Vérifie l'adresse ou inscris-toi.";
        if (b.Contains("smtp") || b.Contains("mail") && b.Contains("fail") || b.Contains("send") && b.Contains("fail"))
            return "Envoi d'email impossible pour le moment (service mail indisponible). Réessaie plus tard.";
        if (b.Contains("invalid email") || b.Contains("malformed"))
            return "Adresse email refusée par le serveur, vérifie-la et réessaie.";
        return "Une erreur est survenue. Vérifie ta connexion et réessaie.";
    }
}
