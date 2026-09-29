namespace SMSR.App.Mvp;

internal static class GraphCycleDetector
{
    public static GraphCycles Find(IReadOnlyList<(string Source, string Target)> edges, int revision)
    {
        var forward = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var reverse = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var selfLoops = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (source, target) in edges)
        {
            if (!forward.TryGetValue(source, out var outgoing)) forward[source] = outgoing = [];
            if (!forward.ContainsKey(target)) forward[target] = [];
            if (!reverse.TryGetValue(target, out var incoming)) reverse[target] = incoming = [];
            if (!reverse.ContainsKey(source)) reverse[source] = [];
            outgoing.Add(target);
            incoming.Add(source);
            if (source == target) selfLoops.Add(source);
        }
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var finished = new List<string>(forward.Count);
        foreach (var root in forward.Keys)
        {
            if (!visited.Add(root)) continue;
            var stack = new Stack<(string Node, int Next)>();
            stack.Push((root, 0));
            while (stack.Count > 0)
            {
                var (node, next) = stack.Pop();
                var neighbors = forward[node];
                if (next == neighbors.Count) { finished.Add(node); continue; }
                stack.Push((node, next + 1));
                if (visited.Add(neighbors[next])) stack.Push((neighbors[next], 0));
            }
        }
        visited.Clear();
        var groups = new List<GraphCycleGroup>();
        for (var i = finished.Count - 1; i >= 0; i--)
        {
            var root = finished[i];
            if (!visited.Add(root)) continue;
            var component = new List<string>();
            var stack = new Stack<string>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                component.Add(node);
                foreach (var previous in reverse[node])
                    if (visited.Add(previous)) stack.Push(previous);
            }
            if (component.Count > 1 || selfLoops.Contains(root))
                groups.Add(new(component.Order(StringComparer.Ordinal).Take(20).ToArray(), component.Count));
        }
        var ordered = groups.OrderByDescending(group => group.NodeCount).ToArray();
        return new(revision, ordered.Take(20).ToArray(), ordered.Length, forward.Count, edges.Count, ordered.Length > 20);
    }
}
