using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphProjectLoadSelfCheck
{
    internal static async Task RunAsync(EventStore store, string root, GraphIndexResult indexed)
    {
        const string project = "project-check";
        var edges = (await store.GetGraphRevisionEdgesAsync(project, indexed.Revision)).ToHashSet();
        var calls = edges.Where(e => e.Relation == "CALLS" && e.Resolution == "RESOLVED" && e.SourceId != e.TargetId)
            .OrderBy(e => e.SourceId, StringComparer.Ordinal).Take(20).ToArray();
        if (calls.Length != 20) throw new Exception("Need twenty real call relationships");
        var query = new GraphQueryService(store);
        var times = new ConcurrentBag<double>();
        var repeat = new GraphIndexService(store).IndexAsync(project, root);
        await Parallel.ForEachAsync(Enumerable.Range(0, 80), new ParallelOptions { MaxDegreeOfParallelism = 8 }, async (i, ct) =>
        {
            var edge = calls[i / 4]; var watch = Stopwatch.StartNew();
            switch (i % 4)
            {
                case 0:
                    GraphProjectLoadOracle.Relations(await query.RelationsAsync(project, edge.SourceId, relation: "CALLS", ct: ct), edges);
                    break;
                case 1:
                    GraphProjectLoadOracle.Trace(await query.TraceAsync(project, edge.SourceId, edge.TargetId, "CALLS", true, ct: ct), edges);
                    break;
                case 2:
                    GraphProjectLoadOracle.Impact(await query.ImpactAsync(project, edge.TargetId, ct: ct, relation: "CALLS"), edge.TargetId, edges);
                    break;
                default:
                    var search = await query.SearchAsync(project, "", ct: ct, offset: i);
                    if (search.Revision != indexed.Revision || search.Nodes.Count == 0) throw new Exception("Search page mismatch");
                    break;
            }
            times.Add(watch.Elapsed.TotalMilliseconds);
        });
        var unchanged = await repeat;
        if (!unchanged.Unchanged || unchanged.Revision != indexed.Revision) throw new Exception("Concurrent unchanged index drift");
        var service = new GraphAdvancedService(store, new GraphWorker());
        var timer = Stopwatch.StartNew();
        var cypher = await service.CypherAsync(project, "MATCH(n:Node) RETURN count(n)");
        if (cypher.GetProperty("rows")[0][0].GetInt32() != indexed.NodeCount) throw new Exception("Cypher count mismatch");
        var report = new { root, indexed, concurrency = 8, samples = times.Count,
            p95Ms = times.Order().ElementAt(75), maxMs = times.Max(), cypherMs = timer.Elapsed.TotalMilliseconds,
            body = await GraphBodyLoadSelfCheck.RunAsync(store, service),
            hostPeakBytes = Process.GetCurrentProcess().PeakWorkingSet64,
            limitation = "service/SQLite timings; unchanged reindex only; host memory excludes Python children" };
        await File.WriteAllTextAsync(Path.Combine(Path.GetTempPath(), "smsr-p2-project-load.json"), JsonSerializer.Serialize(report));
    }
}
