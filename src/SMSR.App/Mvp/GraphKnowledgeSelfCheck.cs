using System.IO;

namespace SMSR.App.Mvp;

internal static class GraphKnowledgeSelfCheck
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "smsr-knowledge-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            await GraphKnowledgeMigrationSelfCheck.RunAsync(root);
            GraphStructuralEvidenceSelfCheck.Run();
            GraphStructureSelfCheck.Run();
            var store = new EventStore(Path.Combine(root, "smsr.db"));
            await store.InitializeAsync();
            await store.ApplyGraphScanAsync("knowledge", GraphKnowledgeFixture.Scan(root), false);
            var query = new GraphQueryService(store);
            var node = await store.GetGraphNodeAsync("knowledge", "method", revision: 1);
            var edge = (await store.GetGraphRevisionEdgesAsync("knowledge", 1)).Single();
            if (node?.Details != GraphKnowledgeFixture.Nodes[3].Details || edge.Evidence?.SourceHash != GraphKnowledgeFixture.Hash
                || edge.Evidence.CapturedAt is null) throw new Exception("Node/evidence roundtrip failed");
            var group = (await query.HyperedgesAsync("knowledge", "method")).Items.Single();
            if (group.Members[2].Role != "processor" || group.Evidence.CapturedAt is null)
                throw new Exception("Group member/evidence roundtrip failed");
            var coverage = await query.CoverageAsync("knowledge", limit: 1);
            if (coverage.TotalFiles != 2 || coverage.DetailedSymbols != 3 || !coverage.Truncated
                || coverage.EdgesWithoutEvidence != 0 || coverage.MetadataUpgradeRequired)
                throw new Exception("Coverage summary/pagination failed");
            await GraphKnowledgeAtomicSelfCheck.RunAsync(store, root);
            await GraphKnowledgeHttpSelfCheck.RunAsync(root);
            await store.ApplyGraphScanAsync("knowledge", new(root, [GraphKnowledgeFixture.Files[0]],
                [], [], [], [], [], ["B.cs"], 1), false);
            if ((await query.HyperedgesAsync("knowledge")).Items.Count != 0
                || (await query.HyperedgesAsync("knowledge", expectedRevision: 1)).Items.Count != 1)
                throw new Exception("Deleted member invalidation/history retention failed");
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(root, true); }
    }
}
