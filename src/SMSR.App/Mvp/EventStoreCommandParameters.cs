using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    private static void AddEventParameters(SqliteCommand command, RecordEventRequest request, string createdAt)
    {
        var values = new Dictionary<string, object?>
        {
            ["$eventId"] = request.EventId, ["$projectId"] = request.ProjectId, ["$workflowId"] = request.WorkflowId,
            ["$nodeId"] = request.NodeId, ["$agentId"] = request.AgentId, ["$eventType"] = request.EventType,
            ["$status"] = request.Status, ["$summary"] = request.Summary, ["$error"] = request.Error,
            ["$payload"] = JsonSerializer.Serialize(request), ["$createdAt"] = createdAt
        };
        foreach (var (name, value) in values) command.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }

    private static void AddStateParameters(SqliteCommand command, RecordEventRequest request, string createdAt)
    {
        command.Parameters.AddWithValue("$projectId", request.ProjectId);
        command.Parameters.AddWithValue("$workflowId", request.WorkflowId);
        command.Parameters.AddWithValue("$nodeId", request.NodeId);
        command.Parameters.AddWithValue("$agentId", request.AgentId);
        command.Parameters.AddWithValue("$status", request.Status);
        command.Parameters.AddWithValue("$summary", request.Summary ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$error", request.Error ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$createdAt", createdAt);
    }
}
