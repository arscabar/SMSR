using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SMSR.App.Services;

internal static partial class CodexAutoTrackingHook
{
    private const string Marker = "SMSR automatic tracking";
    private static readonly string[] OwnedEvents = ["SessionStart", "SessionEnd", "UserPromptSubmit", "PostToolUse", "Stop", "SubagentStart", "SubagentStop"];

    public static bool IsRegistered(string configPath) => IsRegistered(configPath, CurrentExecutable());
    public static string? Register(string configPath) => Register(configPath, CurrentExecutable());

    internal static bool IsRegistered(string configPath, string executable)
    {
        try
        {
            var hooks = Load(HooksPath(configPath))["hooks"] as JsonObject;
            var command = BuildCommand(executable);
            return hooks is not null
                && OwnedEvents.All(name => HasOwnedEntry(hooks[name] as JsonArray, name, command));
        }
        catch { return false; }
    }

    internal static string? Register(string configPath, string executable)
    {
        if (IsRegistered(configPath, executable)) return null;
        var path = HooksPath(configPath);
        var root = Load(path);
        var hooks = root["hooks"] as JsonObject ?? new JsonObject();
        root["hooks"] = hooks;
        var command = BuildCommand(executable);
        foreach (var name in OwnedEvents) SetOwnedEntry(hooks, name, command);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".smsr.tmp";
        File.WriteAllText(temporary, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
        if (!File.Exists(path)) { File.Move(temporary, path); return null; }
        var backup = path + ".smsr.bak";
        File.Replace(temporary, path, backup, true);
        return backup;
    }

    private static JsonObject Load(string path)
    {
        if (!File.Exists(path)) return new JsonObject();
        return JsonNode.Parse(File.ReadAllText(path)) as JsonObject
            ?? throw new InvalidOperationException("Codex hooks.json의 루트는 JSON 객체여야 합니다.");
    }

    private static string HooksPath(string configPath) => Path.Combine(Path.GetDirectoryName(configPath)!, "hooks.json");
    private static string BuildCommand(string executable) => $"\"{Path.GetFullPath(executable)}\" --smsr-auto-track-hook";
    private static string CurrentExecutable() => Environment.ProcessPath
        ?? throw new InvalidOperationException("SMSR 실행 파일 경로를 확인할 수 없습니다.");
}
