using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<GraphIndexResult> ApplyGraphScanAsync(string projectId, GraphScan scan,
        bool allowLargeReduction, CancellationToken ct = default)
    {
        await _writeGate.WaitAsync(ct);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);
            using (var keys = connection.CreateCommand())
            {
                keys.CommandText = "PRAGMA foreign_keys=ON;";
                await keys.ExecuteNonQueryAsync(ct);
            }
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(ct);
            var revision = 0;
            var scopeChanged = false;
            using (var existing = GraphSql.Create(connection, transaction,
                "SELECT root_path,revision,scope_json FROM graph_projects WHERE project_id=$p0;", projectId))
            await using (var reader = await existing.ExecuteReaderAsync(ct))
                if (await reader.ReadAsync(ct))
                {
                    if (!string.Equals(reader.GetString(0), scan.RootPath, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("이 projectId는 다른 저장소에 연결돼 있습니다.");
                    revision = reader.GetInt32(1);
                    scopeChanged = reader.GetString(2) != JsonSerializer.Serialize(scan.Folders ?? []);
                }
            if (scan.ExpectedRevision.HasValue && scan.ExpectedRevision.Value != revision)
                throw new InvalidOperationException("다른 색인이 먼저 반영됐습니다. 최신 소스로 다시 색인하세요.");
            var oldNodes = await GraphSql.CountAsync(connection, transaction,
                "SELECT COUNT(*) FROM graph_nodes WHERE project_id=$p0;", ct, projectId);
            if (revision > 0 && scan.ChangedPaths.Count == 0 && scan.RemovedPaths.Count == 0 && !scopeChanged)
                return new(projectId, revision, scan.Files.Count, (int)oldNodes,
                    (int)await GraphSql.CountAsync(connection, transaction,
                        "SELECT COUNT(*) FROM graph_edges WHERE project_id=$p0;", ct, projectId), 0, 0, true);
            foreach (var path in scan.RemovedPaths)
            {
                await GraphSql.ExecuteAsync(connection, transaction, "DELETE FROM graph_issues WHERE project_id=$p0 AND owner_path=$p1;", ct, projectId, path);
                await GraphSql.ExecuteAsync(connection, transaction, "DELETE FROM graph_edges WHERE project_id=$p0 AND owner_path=$p1;", ct, projectId, path);
                await GraphSql.ExecuteAsync(connection, transaction, "DELETE FROM graph_nodes WHERE project_id=$p0 AND owner_path=$p1;", ct, projectId, path);
                await GraphSql.ExecuteAsync(connection, transaction, "DELETE FROM graph_files WHERE project_id=$p0 AND path=$p1;", ct, projectId, path);
            }
            foreach (var path in scan.ChangedPaths.Concat(scan.ReparsedDocs).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                await GraphSql.ExecuteAsync(connection, transaction, "DELETE FROM graph_issues WHERE project_id=$p0 AND owner_path=$p1;", ct, projectId, path);
                await GraphSql.ExecuteAsync(connection, transaction, "DELETE FROM graph_edges WHERE project_id=$p0 AND owner_path=$p1;", ct, projectId, path);
                await GraphSql.ExecuteAsync(connection, transaction, "DELETE FROM graph_nodes WHERE project_id=$p0 AND owner_path=$p1 AND kind IN ('heading','api_route','mcp_tool');", ct, projectId, path);
            }
            foreach (var file in scan.Files.Where(file => scan.ChangedPaths.Contains(file.Path, StringComparer.OrdinalIgnoreCase)))
                await GraphSql.ExecuteAsync(connection, transaction, """
                    INSERT INTO graph_files(project_id,path,content_hash,kind) VALUES ($p0,$p1,$p2,$p3)
                    ON CONFLICT(project_id,path) DO UPDATE SET content_hash=$p2,kind=$p3;
                    """, ct, projectId, file.Path, file.Hash, file.Kind);
            foreach (var node in scan.Nodes)
                await GraphSql.ExecuteAsync(connection, transaction, """
                    INSERT INTO graph_nodes(project_id,node_id,owner_path,kind,label,source_path,source_line,content_hash)
                    VALUES ($p0,$p1,$p2,$p3,$p4,$p5,$p6,$p7)
                    ON CONFLICT(project_id,node_id) DO UPDATE SET kind=$p3,label=$p4,source_line=$p6,content_hash=$p7;
                    """, ct, projectId, node.NodeId, node.OwnerPath, node.Kind, node.Label,
                    node.SourcePath, node.Line, node.Hash);
            foreach (var edge in scan.Edges)
                await GraphSql.ExecuteAsync(connection, transaction, """
                    INSERT OR IGNORE INTO graph_edges(project_id,source_id,target_id,relation,owner_path,source_line,resolution,confidence)
                    VALUES ($p0,$p1,$p2,$p3,$p4,$p5,$p6,$p7);
                    """, ct, projectId, edge.SourceId, edge.TargetId, edge.Relation, edge.OwnerPath,
                    edge.SourceLine, edge.Resolution, edge.Confidence);
            foreach (var issue in scan.Issues)
                await GraphSql.ExecuteAsync(connection, transaction, """
                    INSERT OR IGNORE INTO graph_issues(project_id,owner_path,source_line,relation,reason,candidates_json)
                    VALUES ($p0,$p1,$p2,$p3,$p4,$p5);
                    """, ct, projectId, issue.OwnerPath, issue.SourceLine, issue.Relation, issue.Reason,
                    JsonSerializer.Serialize(issue.CandidatePaths));
            var nodeCount = await GraphSql.CountAsync(connection, transaction,
                "SELECT COUNT(*) FROM graph_nodes WHERE project_id=$p0;", ct, projectId);
            if (oldNodes >= 20 && nodeCount * 2 < oldNodes && !allowLargeReduction)
                throw new InvalidOperationException("색인이 절반 이상 줄어 기존 데이터가 유지됐습니다. 의도한 변경이라면 allowLargeReduction을 명시하세요.");
            using (var check = GraphSql.Create(connection, transaction, "PRAGMA foreign_key_check(graph_edges);"))
            await using (var reader = await check.ExecuteReaderAsync(ct))
                if (await reader.ReadAsync(ct)) throw new InvalidOperationException("색인에 고아 관계가 있어 저장을 취소했습니다.");
            revision++;
            await GraphSql.ExecuteAsync(connection, transaction,
                "INSERT OR REPLACE INTO graph_index_formats(project_id,format) VALUES ($p0,$p1);", ct, projectId, GraphFormat);
            await GraphSql.ExecuteAsync(connection, transaction, """
                INSERT INTO graph_projects(project_id,root_path,revision,indexed_at_utc,scope_json) VALUES ($p0,$p1,$p2,$p3,$p4)
                ON CONFLICT(project_id) DO UPDATE SET revision=$p2,indexed_at_utc=$p3,scope_json=$p4;
                """, ct, projectId, scan.RootPath, revision, DateTimeOffset.UtcNow.ToString("O"),
                JsonSerializer.Serialize(scan.Folders ?? []));
            await GraphSql.ExecuteAsync(connection, transaction, """
                INSERT INTO graph_revision_nodes
                SELECT project_id,$p1,node_id,owner_path,kind,label,source_path,source_line,content_hash
                FROM graph_nodes WHERE project_id=$p0;
                """, ct, projectId, revision);
            await GraphSql.ExecuteAsync(connection, transaction, """
                INSERT INTO graph_revision_edges
                SELECT project_id,$p1,source_id,target_id,relation,owner_path,source_line,resolution,confidence
                FROM graph_edges WHERE project_id=$p0;
                """, ct, projectId, revision);
            var edgeCount = await GraphSql.CountAsync(connection, transaction,
                "SELECT COUNT(*) FROM graph_edges WHERE project_id=$p0;", ct, projectId);
            await transaction.CommitAsync(ct);
            return new(projectId, revision, scan.Files.Count, (int)nodeCount, (int)edgeCount,
                scan.ChangedPaths.Count, scan.RemovedPaths.Count, false);
        }
        finally { _writeGate.Release(); }
    }
}
