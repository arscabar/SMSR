using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

internal static class GraphRevisionSelfCheck
{
    public static async Task RunAsync(EventStore store, string root, GraphIndexService index)
    {
        var before = (await store.GetGraphInfoAsync("case"))!.Revision;
        await using (var db = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = System.IO.Path.Combine(root, "test.db"), Pooling = false }.ToString()))
        {
            await db.OpenAsync();
            using var command = db.CreateCommand();
            command.CommandText = "DELETE FROM graph_index_formats WHERE project_id='case';";
            await command.ExecuteNonQueryAsync();
        }
        if (!(await index.CheckFreshnessAsync("case")).IsStale) throw new Exception("이전 파서 색인 갱신 필요 표시 실패");
        var rebuilt = await index.IndexAsync("case", root);
        if (rebuilt.Unchanged || rebuilt.Revision != before + 1) throw new Exception("파서 버전별 재색인 실패");
        try
        {
            await store.ApplyGraphScanAsync("case", new(root,[],[],[],[],[],[],[],before), false);
            throw new Exception("오래된 스캔이 최신 색인을 덮어씀");
        }
        catch (InvalidOperationException error) when (error.Message.Contains("다른 색인")) { }
        await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(root,"a.md"), "no link\n");
        await index.IndexAsync("case", root);
        var oldEdges = await store.GetGraphAdjacentAsync("case", ["file:a.md"], false, revision: before);
        var newEdges = await store.GetGraphAdjacentAsync("case", ["file:a.md"], false);
        if (oldEdges.Count != 1 || newEdges.Count != 0) throw new Exception("리비전 고정 조회 격리 실패");
    }
}
