namespace SMSR.App.Mvp;

public sealed record GraphRoleEvidence(string Id, string Path, int Line, string Quote,
    string Hash, GraphEdge? Edge = null);
public sealed record GraphRoleContext(GraphNode Node, int Revision, string Fingerprint,
    IReadOnlyList<GraphRoleEvidence> Evidence, IReadOnlyList<GraphRoleLink> Links,
    IReadOnlyList<string> Unavailable, bool Truncated);
public sealed record GraphRoleLink(GraphEdge Edge, GraphNode From, GraphNode To);
public sealed record GraphRoleClaim(string Kind, string Text, string Confidence,
    IReadOnlyList<string> EvidenceIds, string SupportingQuote);
public sealed record GraphRoleRequest(string ProjectId, string NodeId, int ExpectedRevision,
    string Fingerprint, string Model, IReadOnlyList<GraphRoleClaim> Claims);
public sealed record GraphRoleReport(string NodeId, string Fingerprint, int Revision,
    string Model, IReadOnlyList<GraphRoleClaim> Claims, IReadOnlyList<GraphRoleEvidence> Evidence,
    DateTimeOffset CreatedAt, string Contract = "role-v2");
public sealed record GraphRoleStatus(string Status, GraphRoleReport? Report, string? Reason = null);
