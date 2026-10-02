using System.IO;

namespace SMSR.App.Mvp;

internal static class GraphFilePolicy
{
    private static readonly HashSet<string> SecretNames = new(StringComparer.OrdinalIgnoreCase)
        { "password", "passwords", "token", "tokens", "apikey", "api-key", "api_key", "private-key", "private_key" };

    internal static bool Sensitive(string path)
    {
        var name = Path.GetFileName(path);
        var data = Path.GetExtension(name).ToLowerInvariant() is ".txt" or ".json" or ".yaml" or ".yml" or ".toml" or ".xml" or ".ini";
        return data && SecretNames.Contains(Path.GetFileNameWithoutExtension(name))
            || name.StartsWith(".env", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase)
            || name.Contains("credential", StringComparison.OrdinalIgnoreCase)
            || name.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || name.Equals("config.toml", StringComparison.OrdinalIgnoreCase)
            || name.Equals("settings.local.json", StringComparison.OrdinalIgnoreCase);
    }
}
