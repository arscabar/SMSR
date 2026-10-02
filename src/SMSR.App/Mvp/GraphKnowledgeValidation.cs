using System.Text.RegularExpressions;

namespace SMSR.App.Mvp;

internal static class GraphKnowledgeValidation
{
    private static readonly Regex Hash = new("^[A-Fa-f0-9]{64}$", RegexOptions.Compiled);
    internal static void Node(GraphNode node)
    {
        if (node.Details is not { } d) return;
        if (d.EntityKind is not ("class" or "method" or "function" or "interface" or "struct" or "enum"
            or "property" or "field" or "enum_member" or "namespace" or "concept" or "requirement" or "rationale" or "unknown")
            || d.FileType is not ("code" or "document" or "paper" or "image" or "audio" or "video" or "rationale" or "concept")
            || d.EndLine is { } end && (end < node.Line || end > 1_000_000)
            || d.OwnerNodeId is { } owner && (owner.Length is < 1 or > 1024 || owner == node.NodeId)
            || d.Rationale is { } why && (why.Length > 512 || GraphBodyChunks.Excluded(why))
            || d.SourceLocation is{}location&&(location.Length>256||location.Any(char.IsControl)||GraphBodyChunks.Excluded(location)))
            throw new ArgumentException("심벌 세부 정보가 올바르지 않습니다.");
        Analyzer(d.Analyzer, d.AnalyzerVersion);
    }
    internal static void Evidence(GraphSourceEvidence? evidence)
    {
        if (evidence is null) return;
        Analyzer(evidence.Analyzer, evidence.AnalyzerVersion);
        if (evidence.SourceHash is null || !Hash.IsMatch(evidence.SourceHash) || evidence.ConfidenceScore is { } score
            && (!double.IsFinite(score) || score < 0 || score > 1))
            throw new ArgumentException("관계 원문 지문·추론 점수가 올바르지 않습니다.");
    }
    private static void Analyzer(string analyzer, string version)
    {
        if (string.IsNullOrWhiteSpace(analyzer) || analyzer.Length > 64
            || string.IsNullOrWhiteSpace(version) || version.Length > 64
            || GraphBodyChunks.Excluded(analyzer + " " + version))
            throw new ArgumentException("분석기 정보가 올바르지 않습니다.");
    }
}
