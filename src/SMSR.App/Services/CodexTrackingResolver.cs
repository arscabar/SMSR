using System.Text.Json;
using SMSR.App.Mvp;

namespace SMSR.App.Services;

internal static class CodexTrackingResolver
{
    public static TrackingSession? Resolve(JsonElement input, JsonElement? arguments, string tool)
    {
        if (!IsTrackingTool(tool) || arguments is null) return null;
        var projectId = HookJson.String(arguments.Value, "projectId");
        var workflowId = HookJson.String(arguments.Value, "workflowId");
        if (workflowId.Length == 0 && input.TryGetProperty("tool_response", out var response))
            workflowId = HookJson.FindString(response, "workflowId");
        return projectId.Length == 0 || workflowId.Length == 0 ? null
            : new(projectId, workflowId, HookJson.String(arguments.Value, "nodeId"), DateTimeOffset.UtcNow);
    }

    private static bool IsTrackingTool(string tool)
        => tool.EndsWith("save_plan", StringComparison.OrdinalIgnoreCase)
            || tool.EndsWith("record_event", StringComparison.OrdinalIgnoreCase)
            || tool.EndsWith("record_heartbeat", StringComparison.OrdinalIgnoreCase);
}
