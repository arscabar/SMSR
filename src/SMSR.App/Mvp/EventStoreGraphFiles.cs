using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<IReadOnlyDictionary<string, string>> GetGraphFilesAsync(string projectId, CancellationToken ct = default, int? revision = null)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var command = GraphSql.Create(connection, null,
            revision.HasValue ? "SELECT owner_path,content_hash FROM graph_revision_nodes WHERE project_id=$p0 AND revision=$revision AND node_id='file:'||owner_path;"
                : "SELECT path,content_hash FROM graph_files WHERE project_id=$p0;", projectId);
        if (revision.HasValue) command.Parameters.AddWithValue("$revision", revision.Value);
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) files.Add(reader.GetString(0), reader.GetString(1));
        return files;
    }

    public async Task<GraphInfo?> GetGraphInfoAsync(string projectId, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        return await ReadGraphInfoAsync(connection, null, projectId, ct);
    }

    private static async Task<GraphInfo?> ReadGraphInfoAsync(SqliteConnection connection,
        SqliteTransaction? transaction, string projectId, CancellationToken ct)
    {
        using var command = GraphSql.Create(connection, transaction, """
            SELECT root_path,revision,indexed_at_utc,
              (SELECT COUNT(*) FROM graph_files WHERE project_id=$p0),
              (SELECT COUNT(*) FROM graph_nodes WHERE project_id=$p0),
              (SELECT COUNT(*) FROM graph_edges WHERE project_id=$p0),
              (SELECT COUNT(*) FROM graph_edges WHERE project_id=$p0 AND resolution<>'RESOLVED') +
              (SELECT COUNT(*) FROM graph_issues WHERE project_id=$p0
                AND reason NOT IN ('동일 위치 중복 참조','자기 파일 참조'))
            FROM graph_projects WHERE project_id=$p0;
            """, projectId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? new(projectId, reader.GetString(0), reader.GetInt32(1),
            DateTimeOffset.Parse(reader.GetString(2)), reader.GetInt32(3), reader.GetInt32(4),
            reader.GetInt32(5), reader.GetInt32(6)) : null;
    }
}
