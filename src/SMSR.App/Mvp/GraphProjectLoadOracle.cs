namespace SMSR.App.Mvp;

internal static class GraphProjectLoadOracle
{
    internal static void Relations(GraphRelationPage page, HashSet<GraphEdge> edges)
    {
        var actual = edges.Where(e => e.Relation == "CALLS" && (e.SourceId == page.Node.NodeId || e.TargetId == page.Node.NodeId)).ToArray();
        if (page.Total != actual.Length || page.Items.Length != Math.Min(20, actual.Length)
            || page.Items.Any(i => !actual.Contains(i.Edge) || i.Node.NodeId != (i.Incoming ? i.Edge.SourceId : i.Edge.TargetId)))
            throw new Exception("Relationship page mismatch");
    }
    internal static void Trace(GraphTrace path, HashSet<GraphEdge> edges)
    {
        if (!path.Found || path.Steps.Length != 1 || path.Steps.Any(s => !edges.Contains(s.Edge)
            || s.Source.NodeId != path.Start.NodeId || s.Target.NodeId != path.Target.NodeId || s.Edge.Relation != "CALLS"))
            throw new Exception("Real direct call trace mismatch");
    }
    internal static void Impact(GraphImpact impact, string target, HashSet<GraphEdge> edges)
    {
        var expected = new Dictionary<string, int> { [target] = 0 };
        for (var depth = 1; depth <= 3; depth++)
            foreach (var edge in edges.Where(e => e.Relation == "CALLS"
                && e.Resolution is "RESOLVED" or "INFERRED" && e.Confidence != "AMBIGUOUS"))
                if (expected.TryGetValue(edge.TargetId, out var d) && d == depth - 1) expected.TryAdd(edge.SourceId, depth);
        var steps = impact.Steps ?? throw new Exception("Missing impact provenance");
        var parents = steps.ToDictionary(s => s.NodeId);
        if (steps.Count != impact.Nodes.Count || impact.Nodes.Any(n => !parents.ContainsKey(n.NodeId))
            || expected.Count <= 101 && !expected.Keys.Where(id => id != target).ToHashSet().SetEquals(parents.Keys))
            throw new Exception("Real reverse call set mismatch");
        foreach (var step in steps)
        {
            if (!edges.Contains(step.Edge) || step.Edge.SourceId != step.NodeId || step.Edge.TargetId != step.NextId
                || !expected.TryGetValue(step.NodeId, out var depth) || depth != step.Depth
                || (step.Depth == 1 ? step.NextId != target : !parents.TryGetValue(step.NextId, out var p) || p.Depth != step.Depth - 1))
                throw new Exception("Impact chain/depth mismatch");
        }
    }
}
