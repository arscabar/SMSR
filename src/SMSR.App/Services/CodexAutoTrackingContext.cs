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
            SMSR automatic tracking is enabled; the user does not need to request it. For substantive work, use the connected smsr MCP server with projectId '{projectId}' and workflowId '{workflowId}'. Before substantive tool work, save a concise hierarchical plan and record the coordinator heartbeat. Record meaningful status, progress, retries, next actions, and artifacts as work changes; active agents send their own heartbeat about every 30 seconds and before stopping. Simple answers that require no work need lifecycle tracking only. Never send prompt text, secrets, raw command contents, or every tool call. Do not narrate tracking unless it affects the user.
            """;
        return JsonSerializer.Serialize(new
        {
            hookSpecificOutput = new { hookEventName = "UserPromptSubmit", additionalContext = context }
        });
    }

    private static string Read(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) ? value.GetString() ?? string.Empty : string.Empty;
}
