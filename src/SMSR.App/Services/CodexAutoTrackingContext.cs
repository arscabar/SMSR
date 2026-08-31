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
            SMSR graph tracking is opt-in and ordinary work must create no SMSR records. Use the smsr tools only when the user explicitly asks to track or visualize work as a graph, flow, dashboard, or SMSR workflow, or explicitly asks to resume a previous graph. For a new graph use projectId '{projectId}' and base workflowId '{workflowId}'. To resume, call list_workflows for the project, load the selected workflow with get_plan and get_state, and keep its workflowId. Continue updates until SUCCESS, FAILED, or BLOCKED, send the final event, then stop heartbeats and never attach unrelated requests. Never send prompt text, secrets, raw command contents, or every tool call.
            """;
        return JsonSerializer.Serialize(new
        {
            hookSpecificOutput = new { hookEventName = "UserPromptSubmit", additionalContext = context }
        });
    }

    private static string Read(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) ? value.GetString() ?? string.Empty : string.Empty;
}
