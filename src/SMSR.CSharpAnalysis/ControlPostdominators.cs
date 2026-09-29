namespace SMSR.CSharpAnalysis;

internal static class ControlPostdominators
{
    internal static Dictionary<int, HashSet<int>> Solve(int[] nodes, ControlEdge[] edges, ref int budget)
    {
        var all = nodes.Append(-1).ToArray();
        Take(ref budget, (long)all.Length * all.Length);
        var sets = all.ToDictionary(n => n, n => n == -1 ? new HashSet<int> { -1 } : all.ToHashSet());
        var next = edges.GroupBy(e => e.Source).ToDictionary(g => g.Key, g => g.Select(e => e.Target).Distinct().ToArray());
        // ponytail: bounded set intersection; replace with a tree algorithm only after large-CFG measurements.
        bool changed;
        do
        {
            changed = false;
            foreach (var node in nodes.Reverse())
            {
                Take(ref budget, 1);
                var successors = next[node];
                Take(ref budget, sets[successors[0]].Count);
                var value = new HashSet<int>(sets[successors[0]]);
                foreach (var child in successors.Skip(1))
                {
                    Take(ref budget, value.Count + sets[child].Count);
                    value.IntersectWith(sets[child]);
                }
                value.Add(node);
                if (!value.SetEquals(sets[node])) { sets[node] = value; changed = true; }
            }
        } while (changed);
        return sets;
    }

    internal static void Take(ref int budget, long count)
    {
        if (count > budget) throw new ArgumentException("Control dependence work limit exceeded");
        budget -= (int)count;
    }
}
