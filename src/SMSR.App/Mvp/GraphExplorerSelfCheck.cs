using System.IO;

namespace SMSR.App.Mvp;

internal static class GraphExplorerSelfCheck
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "smsr-explorer-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store = new EventStore(Path.Combine(root, "test.db"));
            await store.InitializeAsync();
            var file = new GraphFile("hub.cs", "A", "code");
            var nodes = Enumerable.Range(0, 122).Select(i => new GraphNode("n" + i, file.Path,
                "symbol", "node" + i, file.Path, i + 1, file.Hash)).ToArray();
            var edges = Enumerable.Range(1, 121).Select(i => new GraphEdge(i == 121 ? "n121" : "n0",
                i == 121 ? "n0" : "n" + i, i % 2 == 0 ? "CALLS" : "REFERENCES", file.Path,
                i, "RESOLVED", i % 3 == 0 ? "INFERRED" : "EXTRACTED")).ToArray();
            await store.ApplyGraphScanAsync("test", new(root, [file], nodes, edges, [], [file.Path], [], []), false);
            var query = new GraphQueryService(store);
            var all = new List<GraphEdge>();
            for (var offset = 0; offset < 121; offset += 20)
            {
                var page = await query.RelationsAsync("test", "n0", offset: offset);
                if (page.Total != 121 || page.Revision != 1) throw new Exception("Relation total/revision mismatch");
                all.AddRange(page.Items.Select(i => i.Edge));
            }
            if (all.Count != 121 || all.Distinct().Count() != 121) throw new Exception("Paged relations missing/duplicated");
            var incoming = await query.RelationsAsync("test", "n0", "incoming");
            var calls = await query.RelationsAsync("test", "n0", "outgoing", "CALLS");
            if (incoming.Total != 1 || !incoming.Items[0].Incoming || calls.Total != 60
                || calls.Items.Any(i => i.Incoming || i.Edge.Relation != "CALLS")) throw new Exception("Relation filter mismatch");
            try { await query.RelationsAsync("test", "n0", expectedRevision: 99); throw new Exception("Stale page accepted"); }
            catch (InvalidOperationException) { }
            try { await query.RelationsAsync("test", "n0", relation: "' OR 1"); throw new Exception("Bad filter accepted"); }
            catch (ArgumentException) { }
            await GraphTraceSelfCheck.RunAsync(store, root);
            await GraphExplanationSelfCheck.RunAsync(store, root);
            await GraphRoleSelfCheck.RunAsync(store, root);
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(root, true); }
    }
}
