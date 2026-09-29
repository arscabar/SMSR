using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SMSR.App.Mvp;

internal static class GraphMarkdown
{
    private static readonly Regex Sensitive = new(@"(?i)(api[_ -]?key|token|password|secret|smds_|sk-[a-z0-9]{10})",
        RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    public static IReadOnlyList<GraphExplicitReference> ExplicitLinks(string markdown)
        => GraphMarkdownSyntax.Parse(markdown).Where(item => item.Kind == "link")
            .Select(item => new GraphExplicitReference(item.Text, item.Line)).ToArray();

    public static (IReadOnlyList<GraphNode> Nodes, IReadOnlyList<GraphEdge> Edges, IReadOnlyList<GraphIssue> Issues) Extract(
        string root, GraphSourceFile source, IReadOnlySet<string> indexed)
    {
        var path = source.File.Path;
        var fileId = "file:" + path;
        var nodes = new List<GraphNode>();
        var edges = new List<GraphEdge>();
        var issues = new List<GraphIssue>();
        var current = fileId;
        var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var item in GraphMarkdownSyntax.Parse(source.Markdown ?? ""))
        {
            if (item.Kind == "heading")
            {
                var label = item.Text.Trim();
                if (Sensitive.IsMatch(label)) label = "[민감 정보 제외]";
                if (label.Length > 200) label = label[..200];
                var titleHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(label)))[..16];
                occurrences.TryGetValue(titleHash, out var count);
                occurrences[titleHash] = ++count;
                current = $"heading:{path}:{titleHash}:{count}";
                nodes.Add(new(current, path, "heading", label, path, item.Line, source.File.Hash));
                edges.Add(new(fileId, current, "CONTAINS", path, item.Line, "RESOLVED", "EXTRACTED"));
                continue;
            }
            var relation = item.Kind == "citation" ? "CITES_FILE" : "LINKS_TO";
            var resolved = GraphLinkResolver.Resolve(root, path, item.Text, indexed);
            if (resolved.Path is null)
            {
                if (resolved.Reason is not null) issues.Add(new(path, item.Line, relation, resolved.Reason, resolved.Candidates));
            }
            else if (resolved.Path == path) issues.Add(new(path, item.Line, relation, "자기 파일 참조", []));
            else edges.Add(new(current, "file:" + resolved.Path, relation, path, item.Line,
                item.Kind == "citation" ? "FILE_ONLY" : "RESOLVED", "EXTRACTED"));
        }
        foreach (var repeated in edges.GroupBy(edge => edge).Where(group => group.Count() > 1))
            issues.Add(new(path, repeated.Key.SourceLine, repeated.Key.Relation, "동일 위치 중복 참조", []));
        return (nodes, edges.Distinct().ToArray(), issues.Distinct().ToArray());
    }
}
