namespace SMSR.App.Mvp;

internal static class GraphKnowledgeAtomicSelfCheck
{
    internal static async Task RunAsync(EventStore store, string root)
    {
        var group = GraphKnowledgeFixture.Group;
        var invalid = new[] { group with { Members = [new("class"), new("method"), new("missing")] },
            group with { Members = [new("class"), new("class"), new("function")] },
            group with { Evidence = group.Evidence with { SourceHash = new('B', 64) } } };
        foreach (var item in invalid)
        {
            try
            {
                await store.ApplyGraphScanAsync("knowledge", GraphKnowledgeFixture.Scan(root, item), false);
                throw new Exception("Invalid group/evidence committed");
            }
            catch (ArgumentException) { }
            if ((await store.GetGraphInfoAsync("knowledge"))?.Revision != 1
                || (await new GraphQueryService(store).HyperedgesAsync("knowledge")).Items.Single().Members.Count != 3)
                throw new Exception("Rejected transaction destroyed committed data");
        }
        var scan = GraphKnowledgeFixture.Scan(root);
        var edge = GraphKnowledgeFixture.Edge;
        var later = edge with { Evidence = edge.Evidence! with { CapturedAt = DateTimeOffset.UtcNow } };
        if (edge != later || edge.GetHashCode() != later.GetHashCode())
            throw new Exception("Capture timestamp misclassified as structural change");
        var badNode = GraphKnowledgeFixture.Nodes[3] with { Details = GraphKnowledgeFixture.Nodes[3].Details! with { OwnerNodeId = "missing" } };
        try
        {
            await store.ApplyGraphScanAsync("knowledge", scan with {
                Nodes = scan.Nodes.Select(n => n.NodeId == badNode.NodeId ? badNode : n).ToArray() }, false);
            throw new Exception("Orphan owner committed");
        }
        catch (ArgumentException) { }
        var query = new GraphQueryService(store);
        try { await query.HyperedgesAsync("knowledge", offset: -1); throw new Exception("Negative offset accepted"); }
        catch (ArgumentException) { }
        try { await query.HyperedgesAsync("knowledge", expectedRevision: 99); throw new Exception("Invalid revision accepted"); }
        catch (ArgumentException) { }
    }
}
