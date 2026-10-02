namespace SMSR.App.Mvp;

public sealed record GraphEdge(string SourceId, string TargetId, string Relation,
    string OwnerPath, int SourceLine, string Resolution, string Confidence,
    IReadOnlyList<GraphDeepEvidence>? Analysis = null, GraphSourceEvidence? Evidence = null)
{
    public bool Equals(GraphEdge? other) => other is not null && SourceId == other.SourceId && TargetId == other.TargetId
        && Relation == other.Relation && OwnerPath == other.OwnerPath && SourceLine == other.SourceLine
        && Resolution == other.Resolution && Confidence == other.Confidence
        && StableEvidence == other.StableEvidence && (Analysis ?? []).SequenceEqual(other.Analysis ?? []);
    private GraphSourceEvidence? StableEvidence => Evidence is null ? null : Evidence with { CapturedAt = null };
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(HashCode.Combine(SourceId, TargetId, Relation, OwnerPath, SourceLine, Resolution, Confidence));
        foreach (var evidence in Analysis ?? []) hash.Add(evidence);
        hash.Add(StableEvidence);
        return hash.ToHashCode();
    }
}
