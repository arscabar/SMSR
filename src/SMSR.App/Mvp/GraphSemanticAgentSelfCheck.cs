using System.IO;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphSemanticAgentSelfCheck
{
    internal static async Task RunAsync(string root, EventStore store, GraphIndexService index, GraphDocumentService docs)
    {
        using var fixture = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
            "docs/test-data/semantic-agent-2026-10-01.json")));
        var data = fixture.RootElement;
        var path = "agent-plan.md";
        File.Copy(Path.Combine(Environment.CurrentDirectory, data.GetProperty("source").GetString()!), Path.Combine(root, path));
        await index.IndexAsync("documents", root);
        var doc = await docs.ReadAsync("documents", path);
        string Location(int line, string quote) => doc.Blocks.Single(b => b.Line == line && b.Text.Contains(quote)).Location;
        var nodes = data.GetProperty("nodes").EnumerateArray().Select(n => new GraphSemanticNode(n[0].GetString()!,
            n[1].GetString()!, n[2].GetString()!, Location(n[3].GetInt32(), n[4].GetString()!), n[4].GetString()!)).ToArray();
        var edges = data.GetProperty("edges").EnumerateArray().Select(e => new GraphSemanticEdge(e[0].GetString()!,
            e[1].GetString()!, "SEMANTIC_CANDIDATE", Location(e[2].GetInt32(), e[3].GetString()!), e[3].GetString()!, true)).ToArray();
        var service = new GraphSemanticService(store, docs);
        var saved = await service.SubmitAsync(new("documents", path, doc.SourceHash, doc.Revision,
            data.GetProperty("model").GetString()!, "semantic-v1", nodes, edges));
        var status = await service.ReadAsync("documents", path);
        var stored = await store.GetGraphRevisionEdgesAsync("documents", saved.Revision);
        if (status.Stale || status.Report.Nodes.Length != 10 || status.Report.Edges.Length != 9
            || stored.Count(e => e.OwnerPath == path && e.Relation == "SEMANTIC_CANDIDATE" && e.Confidence == "INFERRED") != 9)
            throw new Exception("Real host extraction quote/provenance/candidate acceptance failed");
        if ((await service.SubmitAsync(new("documents", path, doc.SourceHash, saved.Revision,
            saved.Model, "semantic-v1", nodes, edges))).Revision != saved.Revision)
            throw new Exception("Real host extraction cache reuse failed");
        var found = await store.SearchGraphNodesAsync("documents", "", kind:"document", revision:saved.Revision);
        if (found.Count(n => n.OwnerPath == path && n.Kind is "concept" or "requirement" or "rationale") != 10
            || found.Any(n => n.Kind == "heading")) throw new Exception("Document filter lost semantic nodes or exposed headings");
        await GraphSemanticCacheSelfCheck.RunAsync(root,index,docs,service,new("documents",path,doc.SourceHash,
            saved.Revision,saved.Model,"semantic-v1",nodes,edges),saved,Path.Combine(Environment.CurrentDirectory,data.GetProperty("source").GetString()!));
    }
}
