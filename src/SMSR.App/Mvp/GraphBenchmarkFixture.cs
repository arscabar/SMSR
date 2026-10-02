using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

internal static class GraphBenchmarkFixture
{
    internal static async Task CreateAsync(string database)
    {
        await new EventStore(database).InitializeAsync();
        await using var connection=new SqliteConnection(new SqliteConnectionStringBuilder { DataSource=database,Pooling=false }.ToString());
        await connection.OpenAsync();
        using var transaction=connection.BeginTransaction();using var command=connection.CreateCommand();
        command.Transaction=transaction;
        command.CommandText="""
            INSERT INTO graph_projects(project_id,root_path,revision,indexed_at_utc)
              VALUES ('bench','synthetic',1,'2026-01-01T00:00:00Z');
            CREATE TEMP TABLE seq AS WITH RECURSIVE n(x) AS
              (SELECT 0 UNION ALL SELECT x+1 FROM n WHERE x<49999) SELECT x FROM n;
            INSERT INTO graph_files(project_id,path,content_hash,kind)
              SELECT 'bench','f'||x,'hash','code' FROM seq WHERE x<5000;
            INSERT INTO graph_nodes(project_id,node_id,owner_path,kind,label,source_path,source_line,content_hash)
              SELECT 'bench','n'||printf('%05d',x),'f'||(x/10),
              'code','Node '||x,'f'||(x/10),1,'hash' FROM seq;
            INSERT INTO graph_edges(project_id,source_id,target_id,relation,owner_path,source_line,resolution,confidence)
              SELECT 'bench','n'||printf('%05d',s.x),
              'n'||printf('%05d',(s.x+o.d)%50000),'REFERENCES','f'||(s.x/10),1,
              'RESOLVED','EXPLICIT' FROM seq s CROSS JOIN
              (SELECT 1 d UNION ALL SELECT 2 UNION ALL SELECT 101 UNION ALL SELECT 1001) o;
            INSERT INTO graph_revision_nodes(project_id,revision,node_id,owner_path,kind,label,source_path,source_line,content_hash)
              SELECT project_id,1,node_id,owner_path,kind,label,source_path,source_line,content_hash FROM graph_nodes;
            INSERT INTO graph_revision_edges(project_id,revision,source_id,target_id,relation,owner_path,source_line,resolution,confidence)
              SELECT project_id,1,source_id,target_id,relation,owner_path,source_line,resolution,confidence FROM graph_edges;
            """;
        await command.ExecuteNonQueryAsync();transaction.Commit();command.Transaction=null;
        command.CommandText="PRAGMA quick_check";
        if((string?)await command.ExecuteScalarAsync()!="ok") throw new Exception("Benchmark database integrity failed");
        command.CommandText="PRAGMA foreign_key_check";
        await using var reader=await command.ExecuteReaderAsync();
        if(await reader.ReadAsync()) throw new Exception("Benchmark foreign key integrity failed");
    }
}
