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

    private static bool IsOwned(JsonObject entry, string name)
        => entry["hooks"] is JsonArray handlers && handlers.OfType<JsonObject>().Any(handler
            => handler["statusMessage"]?.GetValue<string>() == $"{Marker}: {name}");

    private static JsonObject BuildEntry(string name, string command)
    {
        var handlers = new JsonArray();
        if (name == "UserPromptSubmit") handlers.Add(new JsonObject
        {
            ["type"] = "command", ["command"] = command, ["commandWindows"] = command,
            ["timeout"] = 3, ["statusMessage"] = $"{Marker}: context"
        });
        handlers.Add(BuildLifecycle(name));
        return new JsonObject { ["hooks"] = handlers };
    }

    private static JsonObject BuildLifecycle(string name)
    {
        var input = new JsonObject
        {
            ["sessionId"] = "${session_id}", ["cwd"] = "${cwd}", ["eventName"] = LifecycleName(name)
        };
        if (name is "UserPromptSubmit" or "Stop" or "SubagentStart" or "SubagentStop") input["turnId"] = "${turn_id}";
        if (name is "SubagentStart" or "SubagentStop")
        {
            input["agentId"] = "${agent_id}";
            input["agentRole"] = "${agent_type}";
        }
        return new JsonObject
        {
            ["type"] = "mcp_tool", ["server"] = "smsr", ["tool"] = "record_lifecycle",
            ["input"] = input, ["timeout"] = 3, ["statusMessage"] = $"{Marker}: {name}"
        };
    }

    private static string LifecycleName(string name) => name switch
    {
        "SessionStart" => "SESSION_STARTED", "UserPromptSubmit" => "USER_PROMPT",
        "Stop" => "TURN_STOPPED", "SubagentStart" => "SUBAGENT_STARTED",
        _ => "SUBAGENT_STOPPED"
    };
}
