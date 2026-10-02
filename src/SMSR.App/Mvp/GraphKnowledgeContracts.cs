namespace SMSR.App.Mvp;

public sealed record GraphEntityDetails(string EntityKind, string FileType,
    string Analyzer, string AnalyzerVersion, string? OwnerNodeId = null,
    int? EndLine = null, string? Rationale = null,string? SourceLocation=null);
public sealed record GraphSourceEvidence(string Analyzer, string AnalyzerVersion,
    string SourceHash, double? ConfidenceScore = null, DateTimeOffset? CapturedAt = null);
public sealed record GraphHyperedgeMember(string NodeId, string Role = "participant");
public sealed record GraphHyperedge(string HyperedgeId, string Label, string Relation,
    string OwnerPath, int SourceLine, string Confidence, IReadOnlyList<GraphHyperedgeMember> Members,
    GraphSourceEvidence Evidence);
public sealed record GraphHyperedgePage(int Revision, IReadOnlyList<GraphHyperedge> Items,
    bool Truncated);
public sealed record GraphCoverageFile(string Path, string Kind, int Symbols, int DetailedSymbols,
    string Status);
public sealed record GraphCoverage(int Revision, int TotalFiles, int DetailedSymbols,
    int UnknownSymbols, int EdgesWithoutEvidence, bool MetadataUpgradeRequired,
    IReadOnlyList<GraphCoverageFile> Files, bool Truncated);
