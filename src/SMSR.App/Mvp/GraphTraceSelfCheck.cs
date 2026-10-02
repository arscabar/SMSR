namespace SMSR.App.Mvp;

internal static class GraphTraceSelfCheck
{
    internal static async Task RunAsync(EventStore store, string root)
    {
        GraphEdgeSelection.SelfCheck();
        var file = new GraphFile("trace.cs", "A", "code");
        var nodes = Enumerable.Range(0, 5).Select(i => new GraphNode("t" + i, file.Path,
            "symbol", "trace" + i, file.Path, i + 1, file.Hash)).ToArray();
        var edges = new[] { new GraphEdge("t0", "t1", "CALLS", file.Path, 1, "RESOLVED", "EXTRACTED"),
            new("t1", "t2", "CALLS", file.Path, 2, "RESOLVED", "EXTRACTED"),
            new("t2", "t3", "CALLS", file.Path, 3, "INFERRED", "INFERRED"),
            new("t0", "t3", "REFERENCES", file.Path, 4, "RESOLVED", "EXTRACTED"),
            new("t0", "t4", "LINKS_TO", file.Path, 5, "RESOLVED", "EXPLICIT"),
            new("t1", "t4", "CITES_FILE", file.Path, 6, "FILE_ONLY", "EXTRACTED") };
        await store.ApplyGraphScanAsync("trace", new(root, [file], nodes, edges, [], [file.Path], [], []), false);
        var query = new GraphQueryService(store);
        var two = await query.TraceAsync("trace", "t0", "t2", "CALLS");
        if (!two.Found || two.Steps.Length != 2 || two.Steps[1].Source.NodeId != "t1"
            || two.Steps[1].Edge.SourceLine != 2) throw new Exception("Trace continuity/evidence mismatch");
        if ((await query.TraceAsync("trace", "t0", "t3", "CALLS")).Found) throw new Exception("Inferred edge silently included");
        var inferred = await query.TraceAsync("trace", "t0", "t3", "CALLS", true);
        if (!inferred.Found || !inferred.IncludesInferred || inferred.Steps.Length != 3) throw new Exception("Inferred trace mismatch");
        if (!(await query.TraceAsync("trace", "t0", "t3", "CALLS", true, 2)).Truncated
            || (await query.TraceAsync("trace", "t4", "t0")).Truncated
            || !(await query.TraceAsync("trace", "t0", "t0")).Found) throw new Exception("Trace bounds/same-node mismatch");
        var impact = await query.ImpactAsync("trace", "t2", relation: "CALLS", includeInferred: false);
        if (impact.Nodes.Count != 2 || impact.Steps!.Single(s => s.NodeId == "t0").Depth != 2
            || impact.Steps!.Single(s => s.NodeId == "t0").NextId != "t1") throw new Exception("Impact continuity/depth mismatch");
        var explicitPath = await query.TraceAsync("trace", "t0", "t4", "LINKS_TO");
        if (!explicitPath.Found || explicitPath.IncludesInferred) throw new Exception("Explicit document link rejected/mislabeled");
        if (!(await query.TraceAsync("trace", "t1", "t4", "CITES_FILE")).Found) throw new Exception("Explicit file-only reference rejected");
        var reverse = await query.ImpactAsync("trace", "t3", relation: "CALLS", includeInferred: true);
        if (reverse.Nodes.Count != 3 || (await query.ImpactAsync("trace", "t3", relation: "CALLS", includeInferred: false)).Nodes.Count != 0)
            throw new Exception("Inferred impact selection mismatch");
    }
}
