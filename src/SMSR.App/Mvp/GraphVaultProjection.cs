namespace SMSR.App.Mvp;

public sealed partial class GraphVaultService
{
    internal static bool IsFileLink(GraphEdge edge, GraphNode from, GraphNode to)
        => from.SourcePath != to.SourcePath
            && from.Details?.EntityKind != "namespace" && to.Details?.EntityKind != "namespace"
            && (GraphCodeIndex.IsFileDependency(edge.Relation)
                || from.NodeId.StartsWith("file:", StringComparison.Ordinal) && to.NodeId.StartsWith("file:", StringComparison.Ordinal));
    private async Task<Dictionary<string, (string Text, string NodeId)>> NotesAsync(string projectId, CancellationToken ct)
    {
        var data = await new GraphExportService(store, new GraphQueryService(store)).GetAsync(projectId, ct: ct);
        var files = data.Nodes.Where(n => n.NodeId.StartsWith("file:", StringComparison.Ordinal)).ToArray();
        var byPath = files.ToDictionary(n => n.SourcePath, StringComparer.Ordinal);
        var nodes = data.Nodes.ToDictionary(n => n.NodeId);
        var links = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var edge in data.Edges)
        {
            if (!nodes.TryGetValue(edge.SourceId, out var from) || !nodes.TryGetValue(edge.TargetId, out var to)
                || !IsFileLink(edge, from, to) || !byPath.TryGetValue(from.SourcePath, out var a)
                || !byPath.TryGetValue(to.SourcePath, out var b)) continue;
            var evidence = $" · {GraphVaultNote.Text(edge.Relation)} · {GraphVaultNote.Text(edge.Confidence)} · `{edge.OwnerPath.Replace("`", "")}`:{edge.SourceLine}";
            void Add(GraphNode owner, GraphNode other, string direction)
            {
                if (!links.TryGetValue(owner.NodeId, out var values)) links[owner.NodeId] = values = [];
                values.Add("- " + direction + " · " + GraphVaultNote.Link(other) + evidence);
            }
            Add(a, b, "나감"); Add(b, a, "들어옴");
        }
        var notes = new Dictionary<string, (string, string)>();
        foreach (var node in files)
        {
            GraphRoleStatus? role = null;
            if (node.Kind == "code")
                try { role = await roles.ReadAsync(projectId, node.NodeId, ct); }
                catch (Exception e) when (e is ArgumentException or InvalidOperationException or KeyNotFoundException or System.IO.IOException)
                { role = new("STALE", null); }
            notes.Add(GraphVaultPaths.Name(node), (GraphVaultNote.Render(projectId, node, role,
                links.GetValueOrDefault(node.NodeId)?.Order(StringComparer.Ordinal).ToArray() ?? [], SourceAddress()), node.NodeId));
        }
        notes.Add("index.md", ($"# {GraphVaultNote.Text(projectId)} 분석 노트\n\n리비전 {data.Revision} · 파일 {files.Length}\n\n"
            + string.Join("\n", files.OrderBy(n => n.SourcePath, StringComparer.Ordinal).Select(n => "- " + GraphVaultNote.Link(n))) + "\n", "INDEX"));
        if ((await store.GetGraphInfoAsync(projectId, ct))?.Revision != data.Revision)
            throw new InvalidOperationException("노트 생성 중 색인이 변경되었습니다. 다시 동기화하세요.");
        return notes;
    }
}
