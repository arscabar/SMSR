using System.IO;
using System.Text;

namespace SMSR.App.Mvp;

internal static class GraphDeepLanguageSelfCheck
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "smsr-deep-languages-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            await GraphDeepLanguageFixture.InitializeAsync(root);
            var sources = GraphDeepLanguageFixture.Sources;
            foreach (var pair in sources) await File.WriteAllTextAsync(Path.Combine(root, pair.Key), pair.Value, new UTF8Encoding(false));
            var store = new EventStore(Path.Combine(root, "smsr.db"));
            await store.InitializeAsync();
            var baseline = await new GraphIndexService(store).IndexAsync("deep-languages", root);
            var baseEdges = await store.GetGraphRevisionEdgesAsync("deep-languages", baseline.Revision);
            var service = new GraphAdvancedService(store, new GraphWorker());
            await service.AnalyzeCSharpAsync(new("deep-languages", ["Lib.cs"]));
            await Require("csharp");
            await service.AnalyzeJavaAsync(new("deep-languages", ["Lib.java"]));
            await Require("java");
            await service.AnalyzeTypeScriptAsync(new("deep-languages", ["lib.ts"]));
            await Require("typescript");
            await service.AnalyzeAsync("deep-languages", "lib.py");
            await Require("analysis");
            var info = await store.GetGraphInfoAsync("deep-languages") ?? throw new Exception("Graph missing");
            var edges = await store.GetGraphRevisionEdgesAsync("deep-languages", info.Revision);
            if (edges.Where(e => e.Analysis?.Any(a => a.Analyzer is "typescript" or "analysis") == true)
                .Any(e => e.Analysis!.Any(a => a.Dispatch is "STATIC" or "LOCAL_CANDIDATE") && e.Resolution == "RESOLVED" &&
                    !e.Analysis!.Any(a => a.Dispatch == "DIRECT") && !baseEdges.Any(b => b.SourceId == e.SourceId &&
                        b.TargetId == e.TargetId && b.OwnerPath == e.OwnerPath && b.SourceLine == e.SourceLine && b.Resolution == "RESOLVED")))
                throw new Exception("Candidate upgraded existing unresolved relation");
            async Task Require(string kind)
            {
                var status = (await store.GetGraphDeepStatusAsync("deep-languages")).Single(s => s.Analyzer == kind);
                if (status.Status != "APPLIED" || status.Applied < 1) throw new Exception(kind + " actual analysis failed physical declaration mapping: " + status.Status);
            }
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(root, true); }
    }
}
