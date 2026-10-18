namespace Auto237.Config;

/// <summary>
/// Configuration Supabase projet Auto237 (qnsgfykjtddsexvlhlxw, eu-central-1).
/// Anon key publique intégrée pour démo locale. Service_role gardée côté serveur uniquement.
/// </summary>
public static class SupabaseConfig
{
    public const string Url = "https://qnsgfykjtddsexvlhlxw.supabase.co";

    public const string AnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InFuc2dmeWtqdGRkc2V4dmxobHh3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3OTE5ODQzNzgsImV4cCI6MjEwNzU2MDM3OH0.HcbDGZC4EKHFlyAS2NhLhNx4Bn_N3uSOKSv0M-fAPk8";

    public const string AnnoncesBucket = "annonces";

    public static bool IsConfigured =>
        !Url.Contains("VOTRE_PROJECT_REF") && !AnonKey.Contains("VOTRE_ANON_KEY");
}

