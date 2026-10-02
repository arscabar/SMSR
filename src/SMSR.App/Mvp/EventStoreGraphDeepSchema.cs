using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

internal static class EventStoreGraphDeepSchema
{
    internal static async Task CreateAsync(SqliteConnection connection, CancellationToken ct)
    {
        await GraphDeepMigration.BackupAsync(connection, ct);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(ct);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS graph_deep_edges (
              project_id TEXT NOT NULL, revision INTEGER NOT NULL,
              source_id TEXT NOT NULL, target_id TEXT NOT NULL, relation TEXT NOT NULL,
              owner_path TEXT NOT NULL, source_line INTEGER NOT NULL,
              resolution TEXT NOT NULL, confidence TEXT NOT NULL, provenance_json TEXT NOT NULL,
              PRIMARY KEY(project_id,revision,source_id,target_id,relation,owner_path,source_line));
            CREATE INDEX IF NOT EXISTS ix_graph_deep_target ON graph_deep_edges(project_id,revision,target_id);
            CREATE TABLE IF NOT EXISTS graph_deep_status (
              project_id TEXT NOT NULL, kind TEXT NOT NULL, item_key TEXT NOT NULL,
              status TEXT NOT NULL, applied INTEGER NOT NULL, excluded INTEGER NOT NULL,
              PRIMARY KEY(project_id,kind,item_key));
            CREATE TABLE IF NOT EXISTS graph_deep_manifests (
              project_id TEXT NOT NULL, revision INTEGER NOT NULL, kind TEXT NOT NULL,
              item_key TEXT NOT NULL, manifest_json TEXT NOT NULL,
              PRIMARY KEY(project_id,revision,kind,item_key));
            DROP VIEW IF EXISTS graph_effective_edges;
            CREATE VIEW graph_effective_edges AS
            SELECT b.project_id,b.revision,b.source_id,b.target_id,b.relation,b.owner_path,b.source_line,
              CASE WHEN d.resolution='RESOLVED' THEN d.resolution ELSE b.resolution END resolution,
              CASE WHEN d.resolution='RESOLVED' THEN d.confidence ELSE b.confidence END confidence,d.provenance_json,b.evidence_json
            FROM graph_revision_edges b LEFT JOIN graph_deep_edges d
              ON d.project_id=b.project_id AND d.revision=b.revision AND d.source_id=b.source_id
              AND d.target_id=b.target_id AND d.relation=b.relation AND d.owner_path=b.owner_path AND d.source_line=b.source_line
            UNION ALL
            SELECT d.project_id,d.revision,d.source_id,d.target_id,d.relation,d.owner_path,d.source_line,
              d.resolution,d.confidence,d.provenance_json,NULL evidence_json FROM graph_deep_edges d
            WHERE NOT EXISTS (SELECT 1 FROM graph_revision_edges b WHERE b.project_id=d.project_id AND b.revision=d.revision
              AND b.source_id=d.source_id AND b.target_id=d.target_id AND b.relation=d.relation
              AND b.owner_path=d.owner_path AND b.source_line=d.source_line);
            """;
        await command.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
