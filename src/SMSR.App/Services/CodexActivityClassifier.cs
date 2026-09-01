using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace SMSR.App.Services;

internal static class CodexActivityClassifier
{
    public static string Event(string name) => name switch
    {
        "PostToolUse" => "TOOL_COMPLETED",
        "SubagentStart" => "AGENT_STARTED",
        "SubagentStop" => "AGENT_STOPPED",
        "SessionStart" => "SESSION_STARTED",
        "SessionEnd" => "SESSION_ENDED",
        "UserPromptSubmit" => "TURN_STARTED",
        "Stop" => "TURN_STOPPED",
        _ => "AGENT_ACTIVITY"
    };

    public static string Category(string eventName, string tool) => eventName != "PostToolUse" ? "LIFECYCLE"
        : tool == "Bash" ? "COMMAND" : tool == "apply_patch" ? "FILE_EDIT"
        : tool.Contains("smsr", StringComparison.OrdinalIgnoreCase) ? "SMSR"
        : tool.Contains("test", StringComparison.OrdinalIgnoreCase) ? "VALIDATION" : "TOOL";

    public static bool IsTerminalEvent(string tool, JsonElement? arguments)
        => tool.EndsWith("record_event", StringComparison.OrdinalIgnoreCase) && arguments is { } value
            && HookJson.String(value, "status") is "SUCCESS" or "FAILED" or "BLOCKED";

    public static string Identity(string eventName, string sessionId, string turnId,
        string agentId, string toolName, string toolUseId)
    {
        if (eventName != "PostToolUse" || toolUseId.Length == 0) return Guid.NewGuid().ToString("N");
        var source = string.Join('\n', eventName, sessionId, turnId, agentId, toolName, toolUseId);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)))[..32];
    }
}
