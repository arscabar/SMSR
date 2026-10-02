namespace SMSR.App.Mvp;

internal static class GraphDeepQuerySelfCheck
{
    internal static async Task RunAsync(EventStore store)
    {
        var query = new GraphQueryService(store);
        var page = await query.RelationsAsync("deep", "caller", relation: "CALLS");
        var edge = page.Items.Single().Edge;
        if (page.Revision != 2 || edge.Analysis?.Single().Analyzer != "csharp" || edge.Resolution != "RESOLVED")
            throw new Exception("Relations did not expose deep provenance and binding");
        var manifest = await store.GetGraphDeepManifestAsync("deep", 2, "csharp", "key");
        if (manifest?.InputHashes["A.cs"] != "hash" || manifest.Hash != edge.Analysis.Single().ManifestHash)
            throw new Exception("Deep provenance manifest is incomplete or not immutable");
        var trace = await query.TraceAsync("deep", "caller", "target", "CALLS");
        var impact = await query.ImpactAsync("deep", "target", relation: "CALLS", includeInferred: false);
        var scope = await query.ExportScopeAsync("deep", "caller");
        if (!trace.Found || trace.Steps.Single().Edge.Analysis?.Count != 1 ||
            impact.Steps?.Single().Edge.Analysis?.Count != 1 || scope.Edges.Count != 1 || scope.Edges[0].Analysis?.Count != 1)
            throw new Exception("Deep graph differs between path, impact and export");
        if ((await store.GetGraphRevisionEdgesAsync("deep", 1)).Count != 0 ||
            (await store.GetGraphRevisionEdgesAsync("deep", 2)).Single().Analysis?.Count != 1)
            throw new Exception("Deep integration mutated historical revision");
        var same = (await store.GetGraphRevisionEdgesAsync("deep", 2)).Single();
        if (edge != same || new HashSet<GraphEdge> { edge, same }.Count != 1)
            throw new Exception("Equal provenance produced duplicate exported relationships");
    }
}
