using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace SMSR.App.Mvp;

internal sealed record GraphCodeFacts(GraphNode[] Nodes, GraphEdge[] Edges, GraphIssue[] Issues,
    string[]? ReparsedPaths = null, GraphCodeAnalysis? Analysis = null, GraphHyperedge[]? Hyperedges = null);

internal static class GraphCodeIndex
{
    internal static bool IsFileDependency(string relation) => relation is "CALLS" or "REFERENCES" or "USES" or "IMPORTS" or "IMPORTS_FROM" or "INHERITS" or "IMPLEMENTS"
        or "CODE_BEHIND" or "VIEW_MODEL" or "HANDLES_EVENT" or "BINDS_TO" or "COMMAND_BINDS_TO" or "CONVERTER";
    internal static async Task<GraphCodeFacts> ExtractAsync(string root,
        IReadOnlyList<GraphSourceFile> sources, CancellationToken ct,
        IReadOnlyDictionary<string, string>? previousFiles = null, string? forceFullReason = null)
    {
        if (!sources.Any(item => GraphCodeIndexInput.AffectsCode(item.File.Path))) return new([], [], []);
        using var batch = await GraphCodeBatch.CreateAsync(root, sources, ct);
        var config = string.Join("|", sources.Where(item => GraphCodeIndexInput.IsConfiguration(item.File.Path)).OrderBy(item => item.File.Path)
            .Select(item => item.File.Path + ":" + item.File.Hash));
        var cacheId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(root.ToUpperInvariant() + "|" + config)));
        var result = await new GraphWorker().RunCodeIndexAsync(new { operation = "index-code",
            batchFolder = batch.Folder, batchCount = batch.Count, cacheId,
            forceFullReason,
            previousManifest = (previousFiles ?? new Dictionary<string, string>())
                .Where(item => GraphCodeIndexInput.AffectsCode(item.Key)).ToDictionary(item => item.Key, item => item.Value) }, ct, batch.Count);
        await GraphCodeBatch.VerifyAsync(root, sources, ct);
        var facts = result.Deserialize<GraphCodeFacts>(GraphWorker.Json)
            ?? throw new InvalidOperationException("Graphify 관계 결과를 읽지 못했습니다.");
        var known = sources.ToDictionary(item => item.File.Path, StringComparer.Ordinal);
        var nodes = facts.Nodes.ToDictionary(item => item.NodeId, StringComparer.Ordinal);
        foreach (var node in facts.Nodes)
        {
            GraphKnowledgeValidation.Node(node);
            if (node.Kind != "symbol" || !known.TryGetValue(node.OwnerPath, out var file)
                || node.SourcePath != node.OwnerPath || node.Hash != file.File.Hash
                || !node.NodeId.StartsWith("symbol:", StringComparison.Ordinal) || node.Line < 1)
                throw new InvalidOperationException("Graphify 심벌의 원문 근거가 올바르지 않습니다.");
            if (node.Details?.OwnerNodeId is { } parent && !nodes.ContainsKey(parent)
                && !(parent.StartsWith("file:", StringComparison.Ordinal) && known.ContainsKey(parent[5..])))
                throw new InvalidOperationException("Graphify 심벌 소유자가 색인 범위 밖입니다.");
        }
        string Owner(string id) => nodes.TryGetValue(id, out var node) ? node.OwnerPath
            : id.StartsWith("file:", StringComparison.Ordinal) && known.ContainsKey(id[5..]) ? id[5..]
            : throw new InvalidOperationException("Graphify 관계 대상이 색인 범위 밖입니다.");
        var edges = facts.Edges.ToList();
        foreach (var edge in facts.Edges)
        {
            GraphKnowledgeValidation.Evidence(edge.Evidence);
            if (!known.ContainsKey(edge.OwnerPath) || edge.SourceLine < 1)
                throw new InvalidOperationException("Graphify 관계의 원문 근거가 올바르지 않습니다.");
            if (edge.Evidence is { } evidence && evidence.SourceHash != known[edge.OwnerPath].File.Hash)
                throw new InvalidOperationException("Graphify 관계의 원문 지문이 일치하지 않습니다.");
            var caller = Owner(edge.SourceId);
            var target = Owner(edge.TargetId);
            if (caller != target && IsFileDependency(edge.Relation))
                edges.Add(edge with { SourceId = "file:" + caller, TargetId = "file:" + target });
        }
        var affected = (facts.ReparsedPaths ?? sources.Where(item => GraphCodeIndexInput.Supported(item.File.Path))
            .Select(item => item.File.Path)).ToHashSet(StringComparer.Ordinal);
        if (affected.Any(path => !known.ContainsKey(path) || !GraphCodeIndexInput.Supported(path)))
            throw new InvalidOperationException("Graphify 재분석 범위가 색인 범위 밖입니다.");
        return facts with { Nodes = facts.Nodes.Where(node => affected.Contains(node.OwnerPath)).ToArray(),
            Hyperedges = (facts.Hyperedges ?? []).Where(item => affected.Contains(item.OwnerPath)).ToArray(),
            Edges = edges.Where(edge => affected.Contains(edge.OwnerPath)).Distinct().ToArray(),
            Issues = facts.Issues.Where(issue => known.ContainsKey(issue.OwnerPath)
                && (affected.Contains(issue.OwnerPath) || issue.Relation == "STRUCTURE_UNSUPPORTED")).ToArray() };
    }
}
