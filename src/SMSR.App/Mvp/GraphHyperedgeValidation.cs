using System.Text.RegularExpressions;

namespace SMSR.App.Mvp;

internal static class GraphHyperedgeValidation
{
    internal static void Validate(GraphHyperedge item)
    {
        if (item is null || !Text(item.HyperedgeId, 1024) || !Text(item.Label, 512)
            || !Text(item.OwnerPath, 1024) || item.SourceLine is < 1 or > 1_000_000
            || item.Relation is null || !Regex.IsMatch(item.Relation, "^[A-Z_]{1,64}$")
            || item.Confidence is not ("EXTRACTED" or "INFERRED")
            || item.Members is null || item.Members.Count is < 3 or > 64
            || item.Members.Any(m => m is null || !Text(m.NodeId, 1024) || !Text(m.Role, 64))
            || item.Members.Select(m => m.NodeId).Distinct().Count() != item.Members.Count
            || item.Evidence is null)
            throw new ArgumentException("다중 참여 관계의 ID·참여자·근거가 올바르지 않습니다.");
        GraphKnowledgeValidation.Evidence(item.Evidence);
    }
    private static bool Text(string value, int max)
        => !string.IsNullOrWhiteSpace(value) && value.Length <= max && !GraphBodyChunks.Excluded(value);
}
