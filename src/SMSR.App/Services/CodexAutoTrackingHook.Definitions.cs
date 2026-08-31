using System.Text.Json.Nodes;

namespace SMSR.App.Services;

internal static partial class CodexAutoTrackingHook
{
    private static bool HasOwnedEntry(JsonArray? entries, string name, string command)
    {
        if (entries is null) return false;
        return entries.OfType<JsonObject>().Any(entry => IsOwned(entry, name)
            && (name != "UserPromptSubmit" || entry["hooks"]!.AsArray().OfType<JsonObject>()
                .Any(hook => hook["type"]?.GetValue<string>() == "command"
                    && hook["commandWindows"]?.GetValue<string>() == command)));
    }

    private static void SetOwnedEntry(JsonObject hooks, string name, string command)
    {
        var entries = hooks[name] as JsonArray ?? new JsonArray();
        hooks[name] = entries;
        for (var index = entries.Count - 1; index >= 0; index--)
            if (entries[index] is JsonObject entry && IsOwned(entry, name)) entries.RemoveAt(index);
        entries.Add(BuildEntry(name, command));
    }

    private static void RemoveOwnedEntries(JsonObject hooks, string name)
    {
        if (hooks[name] is not JsonArray entries) return;
        for (var index = entries.Count - 1; index >= 0; index--)
            if (entries[index] is JsonObject entry && IsOwned(entry, name)) entries.RemoveAt(index);
        if (entries.Count == 0) hooks.Remove(name);
    }

    private static bool IsOwned(JsonObject entry, string name)
        => entry["hooks"] is JsonArray handlers && handlers.OfType<JsonObject>().Any(handler
            => handler["statusMessage"]?.GetValue<string>() == $"{Marker}: {name}");

    private static JsonObject BuildEntry(string name, string command)
    {
        var handlers = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "command", ["command"] = command, ["commandWindows"] = command,
                ["timeout"] = 3, ["statusMessage"] = $"{Marker}: {name}"
            }
        };
        return new JsonObject { ["hooks"] = handlers };
    }
}
