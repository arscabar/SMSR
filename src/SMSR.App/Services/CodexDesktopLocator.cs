using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace SMSR.App.Services;

internal sealed record CodexDesktopInstallation(string Version, string Root);

internal static class CodexDesktopLocator
{
    private const string PackagesKey = @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages";
    private const string PackagePrefix = "OpenAI.Codex_";

    public static CodexDesktopInstallation? Find()
    {
        try
        {
            using var packages = Registry.CurrentUser.OpenSubKey(PackagesKey);
            var names = (packages?.GetSubKeyNames() ?? [])
                .Where(name => name.StartsWith(PackagePrefix, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(name => name, StringComparer.OrdinalIgnoreCase);
            foreach (var name in names)
            {
                using var package = packages!.OpenSubKey(name);
                var root = package?.GetValue("PackageRootFolder") as string;
                if (IsCodexRoot(root)) return Create(name, root!);
            }
        }
        catch { }

        foreach (var process in Process.GetProcessesByName("ChatGPT"))
        {
            using (process)
            {
                try
                {
                    var app = Directory.GetParent(process.MainModule!.FileName!)?.Parent;
                    if (IsCodexRoot(app?.FullName)) return Create(app!.Name, app.FullName);
                }
                catch { }
            }
        }
        return null;
    }

    private static CodexDesktopInstallation Create(string packageName, string root)
    {
        var parts = packageName.Split('_');
        var version = parts.Length > 1 ? parts[1] : "설치됨";
        return new(version, root);
    }

    private static bool IsCodexRoot(string? root) => !string.IsNullOrWhiteSpace(root)
        && root.Contains("OpenAI.Codex_", StringComparison.OrdinalIgnoreCase)
        && File.Exists(Path.Combine(root, "app", "ChatGPT.exe"));

    public static string GetConfigPath()
    {
        var home = Environment.GetEnvironmentVariable("CODEX_HOME");
        if (string.IsNullOrWhiteSpace(home))
            home = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
        return Path.Combine(Path.GetFullPath(home), "config.toml");
    }
}
