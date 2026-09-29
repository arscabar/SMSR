using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<IReadOnlyList<(string ProjectId, string RootPath, int Revision)>> GetGraphProjectsAsync(CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT project_id,root_path,revision FROM graph_projects ORDER BY project_id LIMIT 101;";
        var projects = new List<(string, string, int)>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) projects.Add((reader.GetString(0), reader.GetString(1), reader.GetInt32(2)));
        return projects;
    }

    public async Task ReplaceCrossRepoEdgesAsync(IReadOnlyList<GraphCrossRepoEdge> edges,
        IReadOnlyDictionary<string, int> expected, CancellationToken ct = default)
    {
        await _writeGate.WaitAsync(ct);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);
            using var transaction = connection.BeginTransaction();
            await CheckCrossRepoVersionsAsync(connection, transaction, expected, ct);
            using var clear = connection.CreateCommand();
            clear.Transaction = transaction;
            clear.CommandText = "DELETE FROM graph_cross_repo_edges;";
            await clear.ExecuteNonQueryAsync(ct);
            using var insert = GraphSql.Create(connection, transaction, """
                INSERT OR IGNORE INTO graph_cross_repo_edges VALUES ($p0,$p1,$p2,$p3,$p4,$p5);
                """, "", "", "", "", "", 0);
            foreach (var edge in edges)
            {
                insert.Parameters[0].Value = edge.SourceProjectId;
                insert.Parameters[1].Value = edge.SourceId;
                insert.Parameters[2].Value = edge.TargetProjectId;
                insert.Parameters[3].Value = edge.TargetId;
                insert.Parameters[4].Value = edge.OwnerPath;
                insert.Parameters[5].Value = edge.SourceLine;
                await insert.ExecuteNonQueryAsync(ct);
            }
            foreach (var (project, revision) in expected)
                await GraphSql.ExecuteAsync(connection, transaction, """
                    INSERT OR REPLACE INTO graph_derived(project_id,kind,item_key,revision,payload,created_at_utc)
                    VALUES ($p0,'crossrepo','',$p1,$p2,$p3);
                    """, ct, project, revision, System.Text.Json.JsonSerializer.Serialize(expected), DateTimeOffset.UtcNow.ToString("O"));
            transaction.Commit();
        }
        finally { _writeGate.Release(); }
    }

    public async Task<IReadOnlyList<GraphCrossRepoEdge>> GetCrossRepoEdgesAsync(string projectId, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var command = GraphSql.Create(connection, null, """
            SELECT source_project_id,source_id,target_project_id,target_id,owner_path,source_line
            FROM graph_cross_repo_edges WHERE source_project_id=$p0 OR target_project_id=$p0
            ORDER BY source_project_id,owner_path,source_line LIMIT 501;
            """, projectId);
        var edges = new List<GraphCrossRepoEdge>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            edges.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetInt32(5)));
        return edges;
    }
}
