namespace SMSR.App.Mvp;

internal static class GraphEdgeSelection
{
    internal static void Validate(string? relation)
    {
        if (relation is not null && (relation.Length is < 1 or > 48
            || relation.Any(c => !char.IsAsciiLetterUpper(c) && c != '_')))
            throw new ArgumentException("관계 종류가 올바르지 않습니다.");
    }

    internal static bool Matches(GraphEdge edge, string? relation, bool includeInferred)
        => (relation is null || edge.Relation == relation)
            && (edge.Resolution is "RESOLVED" or "FILE_ONLY" && edge.Confidence is "EXTRACTED" or "EXPLICIT"
                || includeInferred && edge.Confidence == "INFERRED"
                    && edge.Resolution is "RESOLVED" or "INFERRED");

    internal static void SelfCheck()
    {
        foreach (var resolution in new[] { "RESOLVED", "INFERRED", "UNRESOLVED" })
            foreach (var confidence in new[] { "EXTRACTED", "EXPLICIT", "INFERRED", "AMBIGUOUS" })
            {
                var edge = new GraphEdge("a", "b", "CALLS", "a.cs", 1, resolution, confidence);
                if (resolution == "UNRESOLVED" || confidence == "AMBIGUOUS")
                    if (Matches(edge, null, true)) throw new Exception("Unresolved/ambiguous edge allowed");
                if (Matches(edge, "LINKS_TO", true)) throw new Exception("Wrong relation selected");
            }
    }
}
