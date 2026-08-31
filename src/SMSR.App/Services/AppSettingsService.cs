using System.IO;
using System.Text.Json;

namespace SMSR.App.Services;

public static class DashboardThemes
{
    public const string Dark = "어두운 테마";
    public const string Light = "밝은 테마";
    public static string Normalize(string? value) => value is Light or "Light" ? Light : Dark;
}

public sealed record AppSettings(
    bool StartServerAutomatically = true,
    bool MinimizeToTray = true,
    string DashboardTheme = DashboardThemes.Dark,
    bool AutomateCodexIntegration = true);

public sealed class AppSettingsService
{
    private readonly string _path;
    public AppSettings Current { get; private set; }
    public string Path => _path;
    public event EventHandler? Changed;

    public AppSettingsService(string? dataPath = null)
    {
        dataPath ??= System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMSR");
        _path = System.IO.Path.Combine(dataPath, "settings.json");
        Current = Load();
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!);
        var temporary = _path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, _path, true);
        Current = settings;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private AppSettings Load()
    {
        try
        {
            if (!File.Exists(_path)) return new();
            var text = File.ReadAllText(_path);
            var loaded = JsonSerializer.Deserialize<AppSettings>(text) ?? new();
            using var document = JsonDocument.Parse(text);
            if (!document.RootElement.TryGetProperty(nameof(AppSettings.AutomateCodexIntegration), out _))
                loaded = loaded with { AutomateCodexIntegration = true };
            return loaded with { DashboardTheme = DashboardThemes.Normalize(loaded.DashboardTheme) };
        }
        catch { return new(); }
    }
}
