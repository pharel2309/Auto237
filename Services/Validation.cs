using System.Text.RegularExpressions;

namespace Auto237.Services;

public static class Validation
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static bool EmailValide(string? email)
        => !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email.Trim());
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
        if (b.Contains("invalid") && b.Contains("email"))
            return "Cet email semble invalide, vérifie le @ et le .com — ex. admin@gmail.com";
        return "Une erreur est survenue. Vérifie ta connexion et réessaie.";
    }
}
