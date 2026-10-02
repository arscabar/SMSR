namespace SMSR.App.Mvp;

public sealed record GraphSemanticNode(string Id, string Kind, string Label, string Location, string Quote, bool Inferred = false);
public sealed record GraphSemanticEdge(string SourceId, string TargetId, string Relation,
    string Location, string Quote, bool Inferred = false);
public sealed record GraphSemanticGroup(string Id, string Label, string Location, string Quote,
    GraphHyperedgeMember[] Members, bool Inferred = false);
public sealed record GraphSemanticRequest(string ProjectId, string Path, string SourceHash, int ExpectedRevision,
    string Model, string ContractVersion, GraphSemanticNode[] Nodes, GraphSemanticEdge[] Edges, GraphSemanticGroup[]? Groups = null);
public sealed record GraphSemanticReport(string Path, string SourceHash, string Model, string ContractVersion,
    int Revision, DateTimeOffset CapturedAt, GraphSemanticNode[] Nodes, GraphSemanticEdge[] Edges,
    IReadOnlyDictionary<string, string> Dependencies, GraphSemanticGroup[]? Groups = null,string? ExtractionFingerprint=null);
public sealed record GraphSemanticStatus(GraphSemanticReport Report, bool Stale, string Status);
