using System.IO;
using System.Text;

namespace SMSR.App.Mvp;

public sealed class GraphEvidenceService(EventStore store)
{
    public async Task<GraphEvidence> GetAsync(string projectId, string workflowId, CancellationToken ct = default)
    {
        if (EventValidation.ValidateWorkflowIds(projectId, workflowId) is { } error)
            throw new ArgumentException(error);
        var info = await store.GetGraphInfoAsync(projectId, ct)
            ?? throw new KeyNotFoundException("이 프로젝트의 관계 색인이 없습니다.");
        var indexed = await store.GetGraphFilesAsync(projectId, ct, info.Revision);
        var evidence = await store.GetEvidenceAsync(projectId, workflowId, ct);
        var selected = evidence.Take(201).ToArray();
        var paths = selected.Select(item => Resolve(info.RootPath, item.Reference, indexed.Keys))
            .Where(path => path is not null).Select(path => "file:" + path).Distinct().ToArray();
        var nodes = (await store.GetGraphNodesAsync(projectId, paths, ct, info.Revision)).ToDictionary(node => node.NodeId);
        var matches = selected.Take(200).Select(item =>
        {
            var path = Resolve(info.RootPath, item.Reference, indexed.Keys);
            var node = path is not null && nodes.TryGetValue("file:" + path, out var found) ? found : null;
            return new GraphEvidenceMatch(item.NodeId, item.EventId, item.Reference, node);
        }).ToArray();
        return new(matches, selected.Length > 200, info.Revision);
    }

    private static string? Resolve(string root, string reference, IEnumerable<string> indexed)
    {
        var value = reference.Trim();
        if (value.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            && !value.StartsWith("file://", StringComparison.OrdinalIgnoreCase)) value = value[5..];
        if (value.Length == 0 || value.Length > 1024) return null;
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme == "file") value = uri.LocalPath;
            else if (!Path.IsPathFullyQualified(value)) return null;
        }
        try
        {
            var full = Path.GetFullPath(Path.IsPathFullyQualified(value) ? value : Path.Combine(root, value));
            if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return null;
            var relative = Path.GetRelativePath(root, full).Replace('\\', '/').Normalize(NormalizationForm.FormC);
            return GraphLinkResolver.CanonicalPath(indexed, relative);
        }
        catch (Exception error) when (error is ArgumentException or PathTooLongException) { return null; }
    }
}
