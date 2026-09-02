using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public Task<IReadOnlyList<string>> GetProjectIdsAsync(CancellationToken cancellationToken = default)
        => GetIdsAsync("SELECT project_id FROM events UNION SELECT project_id FROM plan_nodes UNION SELECT project_id FROM agent_heartbeats ORDER BY project_id LIMIT 200;", null, cancellationToken);

    public Task<IReadOnlyList<string>> GetWorkflowIdsAsync(string projectId, CancellationToken cancellationToken = default)
        => GetIdsAsync("SELECT workflow_id FROM events WHERE project_id = $projectId UNION SELECT workflow_id FROM plan_nodes WHERE project_id = $projectId UNION SELECT workflow_id FROM agent_heartbeats WHERE project_id = $projectId ORDER BY workflow_id LIMIT 200;", projectId, cancellationToken);

    public async Task<bool> WorkflowExistsAsync(string projectId, string workflowId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM plan_nodes WHERE project_id=$projectId AND workflow_id=$workflowId UNION SELECT 1 FROM events WHERE project_id=$projectId AND workflow_id=$workflowId UNION SELECT 1 FROM agent_heartbeats WHERE project_id=$projectId AND workflow_id=$workflowId);";
        command.Parameters.AddWithValue("$projectId", projectId);
        command.Parameters.AddWithValue("$workflowId", workflowId);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    private async Task<IReadOnlyList<string>> GetIdsAsync(string sql, string? projectId, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = sql;
        if (projectId is not null) command.Parameters.AddWithValue("$projectId", projectId);
        var ids = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) ids.Add(reader.GetString(0));
        return ids;
    }
}
