using System.IO;
using System.Text;
using System.Text.Json;

namespace SMSR.App.Services;

internal static class CodexAutoTrackingContext
{
    public static async Task RunAsync()
    {
        using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
        await using var writer = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
        await writer.WriteAsync(CreateOutput(await reader.ReadToEndAsync()));
    }

    internal static string CreateOutput(string input)
    {
        using var document = JsonDocument.Parse(input);
        var root = document.RootElement;
        var cwd = Read(root, "cwd");
        var projectId = Path.GetFileName(cwd.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(projectId)) projectId = "workspace";
        var workflowId = Read(root, "session_id");
        var context = $"""
            SMSR lifecycle reporting is enabled, but graph tracking is opt-in. Use the smsr planning and progress tools only when the user explicitly asks to track or visualize the requested work as a graph, flow, dashboard, or SMSR workflow. For ordinary work, do not call save_plan, record_heartbeat, or record_event. When graph tracking is requested, use projectId '{projectId}' and base workflowId '{workflowId}', keep one stable workflow for that requested scope, and continue updating it until the scope reaches SUCCESS, FAILED, or BLOCKED. Send the final terminal event, then stop graph heartbeats and never attach later unrelated requests to that graph. Related follow-up turns before completion continue the same graph without another request. Never send prompt text, secrets, raw command contents, or every tool call. Do not narrate tracking unless it affects the user.
            """;
        return JsonSerializer.Serialize(new
        {
            hookSpecificOutput = new { hookEventName = "UserPromptSubmit", additionalContext = context }
        });
    }

    private static string Read(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) ? value.GetString() ?? string.Empty : string.Empty;
}
