using System.IO;

namespace SMSR.App.Mvp;

internal static class GraphSemanticSelfCheck
{
    internal static async Task RunAsync(string root, EventStore store, GraphIndexService index, GraphDocumentService docs)
    {
        var doc = await docs.ReadAsync("documents", "plan.md");
        var service = new GraphSemanticService(store, docs);
        var request = GraphSemanticFixture.Request(doc);
        var saved = await service.SubmitAsync(request);
        var nodes = await store.SearchGraphNodesAsync("documents", "이벤트 이력", 20);
        var id = nodes.Single(n => n.Kind == "concept").NodeId;
        if ((await service.SubmitAsync(request with { ExpectedRevision = saved.Revision })).Revision != saved.Revision
            || (await new GraphQueryService(store).HyperedgesAsync("documents", id)).Items.Single().Members.Count != 3)
            throw new Exception("Semantic cache/hyperedge roundtrip failed");
        if ((await new GraphQueryService(store).ContextAsync("documents", id)).Outgoing.Single().Edge.Evidence?.SourceHash != doc.SourceHash)
            throw new Exception("Semantic graph provenance missing");
        await GraphSemanticSafetySelfCheck.RunAsync(service, store, request with { ExpectedRevision = saved.Revision });
        await File.WriteAllTextAsync(Path.Combine(root, "other.md"), "# Unrelated\n"); await index.IndexAsync("documents", root);
        if ((await service.ReadAsync("documents", "plan.md")).Stale
            || !(await new GraphQueryService(store).ContextAsync("documents", id)).Outgoing.Any()
            || (await new GraphQueryService(store).HyperedgesAsync("documents", id)).Items.Count != 1)
            throw new Exception("Unrelated membership change discarded semantic cache/relations");
        await File.AppendAllTextAsync(Path.Combine(root, "src/service.py"), "# changed\n"); await index.IndexAsync("documents", root);
        if (!(await service.ReadAsync("documents", "plan.md")).Stale || await store.GetGraphNodeAsync("documents", id) is not null)
            throw new Exception("Changed semantic dependency not invalidated");
    }
}
