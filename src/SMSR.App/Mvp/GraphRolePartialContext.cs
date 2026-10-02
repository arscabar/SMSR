using System.IO;
using System.Text.RegularExpressions;

namespace SMSR.App.Mvp;

internal static class GraphRolePartialContext
{
    private static MatchCollection Find(string text, string pattern) => Regex.Matches(text, pattern,
        RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, TimeSpan.FromSeconds(1));
    internal static (string Namespace, string Type)? Identity(string text)
    {
        var spaces = Find(text, @"^\s*namespace\s+([\w.]+)\s*[;{]");
        var types = Find(text, @"^\s*(?:public\s+|internal\s+|sealed\s+|abstract\s+|static\s+)*partial\s+(?:class|struct|record)\s+(\w+)");
        var declarations = Find(text, @"\b(?:class|struct|record|interface)\s+\w+");
        return spaces.Count <= 1 && types.Count == 1 && declarations.Count == 1
            && !text[(types[0].Index + types[0].Length)..].TrimStart().StartsWith('<')
            ? (spaces.Count == 0 ? "" : spaces[0].Groups[1].Value, types[0].Groups[1].Value) : null;
    }
    internal static async Task AddAsync(EventStore store, string projectId, int revision,
        GraphNode selected, GraphRoleEvidenceCollector collector, CancellationToken ct)
    {
        if (!selected.SourcePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            || await collector.ReadAsync(selected.SourcePath, ct) is not { } source || Identity(source) is not { } identity) return;
        var names = Find(source, @"\b([A-Za-z_]\w*)\s*\(").Select(m => m.Groups[1].Value).Distinct().Take(33).ToArray();
        if (names.Length > 32) collector.Truncated = true;
        var nodes = await store.GraphRolePartialMembersAsync(projectId, revision, identity.Type, names.Take(32).ToArray(), ct);
        if (nodes.Length > 4) collector.Truncated = true;
        foreach (var node in nodes.Where(n => n.SourcePath != selected.SourcePath).Take(4))
        {
            // Same-folder source confirmation avoids merging equal names across projects/namespaces.
            if (Path.GetDirectoryName(node.SourcePath) != Path.GetDirectoryName(selected.SourcePath)) continue;
            if (await collector.ReadAsync(node.SourcePath, ct) is not { } helper || Identity(helper) != identity) continue;
            await collector.AddAsync(node.SourcePath, node.Line, Math.Clamp((node.Details?.EndLine ?? node.Line + 40) - node.Line + 7, 8, 80), null, ct);
        }
    }
}
