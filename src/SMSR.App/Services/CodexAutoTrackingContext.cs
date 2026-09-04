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

    internal static string CreateOutput(string input, AppSettings? settings = null)
    {
        using var document = JsonDocument.Parse(input);
        var root = document.RootElement;
        var cwd = Read(root, "cwd");
        var projectId = Path.GetFileName(cwd.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(projectId)) projectId = "workspace";
        var workflowId = Read(root, "session_id");
        settings ??= new AppSettingsService().Current;
        var planning = settings.RequirePlanReview
            ? $" User-configured SMSR planning policy: {PlanningPromptSettings.Expand(settings.PlanningPrompt, projectId, workflowId)}"
            : string.Empty;
        var automaticGraph = settings.TrackComplexTasksAutomatically
            ? " Automatically create a graph only for a project-changing request that has at least two complexity signals: three or more execution stages, multiple files or components, build/test/release work, parallel or subagent work, or an expected duration over ten minutes."
            : " Do not automatically create a graph based on complexity; graph tracking requires the user's explicit request.";
        var context = $"""
            SMSR separates daily activity from graphs. Never record calculations, quick lookups, Q&A, read-only inspection, status checks, navigation, or commands that do not change project files. After any request that actually changes project files, call record_daily_activity exactly once before the final reply with a unique activityId for that request, a concise title, result summary, changed file paths, validation results, and artifacts; use the current task/session ID '{workflowId}' as taskId and project folder '{projectId}' as projectId. Reuse the activityId only to correct that same card. Link workflowId only when a graph was used. Never store prompt text or raw commands.{automaticGraph} An explicit user request to track or visualize a graph always overrides the complexity threshold. Ambiguous or single-location small edits do not create graphs and use only record_daily_activity. A graph scope ends when every planned node is terminal; later work starts no graph unless that request independently qualifies or the user explicitly requests one. For a new graph omit workflowId in the first save_plan call and reuse the returned readable ID only for that scope. Update an active graph before related follow-up work, preserve completed nodes, and add new sibling/root nodes with dependsOn. Never reopen or add children to SUCCESS nodes. Send record_event immediately on meaningful progress and heartbeat within 30 seconds only during quiet active work. Complete predecessors before successors; only independent nodes run in parallel. To resume, use list_workflows, get_plan and get_state. Never send secrets, prompt text, raw commands, tool inputs or outputs.{planning}
            """;
        return JsonSerializer.Serialize(new
        {
            hookSpecificOutput = new { hookEventName = "UserPromptSubmit", additionalContext = context }
        });
    }

    private static string Read(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) ? value.GetString() ?? string.Empty : string.Empty;
}
