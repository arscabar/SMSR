using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SMSR.App.Mvp;

internal static class ActivityEndpoints
{
    public static void Map(WebApplication app, ActivityJsonlStore activity, ActivityHookToken token,
        WorkflowEventNotifier notifier)
    {
        app.MapPost("/api/activity", async (HttpRequest request) =>
        {
            if (!token.Validate(request.Headers["X-SMSR-Hook-Token"].ToString()))
                return Results.Unauthorized();
            var record = await request.ReadFromJsonAsync<ActivityRecord>();
            if (Validate(record) is { } error) return Results.BadRequest(new { error });
            var recorded = activity.Append(record!);
            if (recorded) notifier.Publish(record!.ProjectId, record.WorkflowId);
            return Results.Ok(new { recorded });
        });
        app.MapGet("/api/activity", (string? projectId, string? workflowId) =>
            string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(workflowId)
                ? Results.BadRequest(new { error = "projectId와 workflowId가 필요합니다." })
                : Results.Ok(activity.ReadLatest(projectId, workflowId)));
    }

    private static string? Validate(ActivityRecord? record)
    {
        if (record is null) return "활동 데이터가 필요합니다.";
        if (string.IsNullOrWhiteSpace(record.ActivityId) || record.ActivityId.Length > 64
            || string.IsNullOrWhiteSpace(record.ProjectId) || record.ProjectId.Length > 128
            || string.IsNullOrWhiteSpace(record.WorkflowId) || record.WorkflowId.Length > 128
            || string.IsNullOrWhiteSpace(record.SessionId) || record.SessionId.Length > 256
            || string.IsNullOrWhiteSpace(record.Event) || record.Event.Length > 64
            || string.IsNullOrWhiteSpace(record.Category) || record.Category.Length > 64)
            return "활동 식별자가 올바르지 않습니다.";
        if (record.TurnId?.Length > 256 || record.AgentId?.Length > 256 || record.NodeId?.Length > 128
            || record.ToolName?.Length > 256 || record.ToolUseId?.Length > 256)
            return "활동 필드 길이가 올바르지 않습니다.";
        return null;
    }
}
