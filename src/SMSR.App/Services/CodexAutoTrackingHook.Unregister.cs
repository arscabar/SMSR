using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SMSR.App.Services;

internal static partial class CodexAutoTrackingHook
{
    public static string? Unregister(string configPath)
    {
        var path = HooksPath(configPath);
        if (!File.Exists(path)) return null;
        var root = Load(path);
        if (root["hooks"] is not JsonObject hooks) return null;
        var changed = false;

        foreach (var name in Events)
        {
            if (hooks[name] is not JsonArray entries) continue;
            for (var index = entries.Count - 1; index >= 0; index--)
                if (entries[index] is JsonObject entry && IsOwned(entry, name))
                {
                    entries.RemoveAt(index);
                    changed = true;
                }
            if (entries.Count == 0) hooks.Remove(name);
        }
        if (!changed) return null;
        if (hooks.Count == 0) root.Remove("hooks");

        var temporary = path + ".smsr.tmp";
        File.WriteAllText(temporary, root.ToJsonString(
            new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
        var backup = path + ".smsr.bak";
        File.Replace(temporary, path, backup, true);
        return backup;
    }
}
