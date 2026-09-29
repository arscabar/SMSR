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
            INSERT INTO graph_projects VALUES ('bench','synthetic',1,'2026-01-01T00:00:00Z');
            CREATE TEMP TABLE seq AS WITH RECURSIVE n(x) AS
              (SELECT 0 UNION ALL SELECT x+1 FROM n WHERE x<49999) SELECT x FROM n;
            INSERT INTO graph_files SELECT 'bench','f'||x,'hash','code' FROM seq WHERE x<5000;
            INSERT INTO graph_nodes SELECT 'bench','n'||printf('%05d',x),'f'||(x/10),
              'code','Node '||x,'f'||(x/10),1,'hash' FROM seq;
            INSERT INTO graph_edges SELECT 'bench','n'||printf('%05d',s.x),
              'n'||printf('%05d',(s.x+o.d)%50000),'REFERENCES','f'||(s.x/10),1,
              'RESOLVED','EXPLICIT' FROM seq s CROSS JOIN
              (SELECT 1 d UNION ALL SELECT 2 UNION ALL SELECT 101 UNION ALL SELECT 1001) o;
            INSERT INTO graph_revision_nodes SELECT project_id,1,node_id,owner_path,kind,label,source_path,source_line,content_hash FROM graph_nodes;
            INSERT INTO graph_revision_edges SELECT project_id,1,source_id,target_id,relation,owner_path,source_line,resolution,confidence FROM graph_edges;
            """;
        await command.ExecuteNonQueryAsync();transaction.Commit();
    }
}
