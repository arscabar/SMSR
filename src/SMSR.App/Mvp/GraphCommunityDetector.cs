namespace SMSR.App.Mvp;

internal static class GraphCommunityDetector
{
    public static GraphCommunities Find(IReadOnlyList<string> ids,
        IReadOnlyList<(string Source, string Target)> edges, int revision)
    {
        var index = ids.Select((id, position) => (id, position))
            .ToDictionary(item => item.id, item => item.position, StringComparer.Ordinal);
        var adjacent = Enumerable.Range(0, ids.Count).Select(_ => new List<int>()).ToArray();
        foreach (var (source, target) in edges)
        {
            if (!index.TryGetValue(source, out var a) || !index.TryGetValue(target, out var b) || a == b) continue;
            adjacent[a].Add(b);
            adjacent[b].Add(a);
        }
        var membership = Enumerable.Range(0, ids.Count).ToArray();
        var totals = adjacent.Select(list => list.Count).ToArray();
        var twoEdges = totals.Sum();
        if (twoEdges > 0)
            for (var pass = 0; pass < 10; pass++)
            {
                var moved = false;
                for (var node = 0; node < ids.Count; node++)
                {
                    var degree = adjacent[node].Count;
                    if (degree == 0) continue;
                    var current = membership[node];
                    totals[current] -= degree;
                    var counts = new Dictionary<int, int>();
                    foreach (var neighbor in adjacent[node])
                    {
                        var group = membership[neighbor];
                        counts[group] = counts.GetValueOrDefault(group) + 1;
                    }
                    var best = current;
                    var bestGain = 0d;
                    foreach (var (group, linked) in counts)
                    {
                        var gain = linked - (double)degree * totals[group] / twoEdges;
                        if (gain > bestGain + 1e-9 || Math.Abs(gain - bestGain) <= 1e-9 && group < best)
                        { best = group; bestGain = gain; }
                    }
                    membership[node] = best;
                    totals[best] += degree;
                    moved |= best != current;
                }
                if (!moved) break;
            }
        var groups = ids.Select((id, position) => (id, position))
            .Where(item => adjacent[item.position].Count > 0)
            .GroupBy(item => membership[item.position])
            .Select(group => new GraphCommunityGroup(group.Key, group.Count(),
                group.Select(item => item.id).Take(20).ToArray()))
            .OrderByDescending(group => group.NodeCount).ThenBy(group => group.Id).ToArray();
        return new(revision, groups.Take(50).ToArray(), groups.Length,
            adjacent.Count(list => list.Count == 0), ids.Count, edges.Count, groups.Length > 50);
    }
}
