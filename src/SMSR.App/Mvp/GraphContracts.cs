namespace SMSR.App.Mvp;

public sealed record GraphFile(string Path, string Hash, string Kind);
public sealed record GraphNode(string NodeId, string OwnerPath, string Kind, string Label,
    string SourcePath, int Line, string Hash);
public sealed record GraphEdge(string SourceId, string TargetId, string Relation,
    string OwnerPath, int SourceLine, string Resolution, string Confidence);
public sealed record GraphIssue(string OwnerPath, int SourceLine, string Relation, string Reason,
    IReadOnlyList<string> CandidatePaths);
public sealed record GraphScan(string RootPath, IReadOnlyList<GraphFile> Files,
    IReadOnlyList<GraphNode> Nodes, IReadOnlyList<GraphEdge> Edges, IReadOnlyList<GraphIssue> Issues,
    IReadOnlyList<string> ChangedPaths, IReadOnlyList<string> ReparsedDocs,
    IReadOnlyList<string> RemovedPaths, int? ExpectedRevision = null,
    IReadOnlyList<string>? Folders = null);
public sealed record GraphIndexResult(string ProjectId, int Revision, int FileCount,
    int NodeCount, int EdgeCount, int ChangedFiles, int RemovedFiles, bool Unchanged);
public sealed record GraphFreshness(int Revision, bool IsStale, int AddedCount, int ChangedCount,
    int RemovedCount, IReadOnlyList<string> SamplePaths, bool Truncated);
public sealed record GraphInfo(string ProjectId, string RootPath, int Revision,
    DateTimeOffset IndexedAt, int FileCount, int NodeCount, int EdgeCount, int UnresolvedEdges);
public sealed record GraphNeighbor(GraphEdge Edge, GraphNode Node);
public sealed record GraphContext(GraphNode Node, IReadOnlyList<GraphNeighbor> Outgoing,
    IReadOnlyList<GraphNeighbor> Incoming, bool Truncated, int Revision);
public sealed record GraphPath(IReadOnlyList<GraphEdge> Edges, bool Found, bool Truncated,
    int VisitedNodes, int Revision);
public sealed record GraphImpact(IReadOnlyList<GraphNode> Nodes, bool Truncated,
    int Revision);
public sealed record GraphSearch(IReadOnlyList<GraphNode> Nodes, bool Truncated, int Revision);
public sealed record GraphHealth(GraphInfo Info, int DanglingEdges, int SelfLoops,
    int DuplicateReferences, int IssueCount, IReadOnlyList<GraphIssue> Issues);
public sealed record GraphCycleGroup(IReadOnlyList<string> NodeIds, int NodeCount);
public sealed record GraphCycles(int Revision, IReadOnlyList<GraphCycleGroup> Groups,
    int TotalGroups, int ScannedNodes, int ScannedEdges, bool Truncated);
public sealed record GraphNodeChange(string Change, GraphNode? Before, GraphNode? After);
public sealed record GraphEdgeChange(string Change, GraphEdge? Before, GraphEdge? After);
public sealed record GraphDiff(int FromRevision, int ToRevision,
    IReadOnlyList<GraphNodeChange> Nodes, IReadOnlyList<GraphEdgeChange> Edges,
    int NodeChangeCount, int EdgeChangeCount, bool Truncated);
public sealed record GraphFeedbackRequest(string ProjectId, string SourceId, string TargetId,
    string Relation, string OwnerPath, int SourceLine, string Verdict);
public sealed record GraphFeedback(string FeedbackId, string SourceId, string TargetId,
    string Relation, string OwnerPath, int SourceLine, string Verdict,
    DateTimeOffset CreatedAt, bool Stale);
public sealed record GraphRouteMap(int Revision, IReadOnlyList<GraphNode> Routes, bool Truncated);
public sealed record GraphValidationGaps(string WorkflowId, int FileEvidenceCount,
    int UnresolvedEvidenceCount, IReadOnlyList<string> Verifications,
    bool VerificationUnrecorded, bool EvidenceTruncated, int Revision);
public sealed record GraphScopeExport(string ProjectId, int Revision, string RootNodeId,
    string Direction, int Depth, IReadOnlyList<GraphNode> Nodes,
    IReadOnlyList<GraphEdge> Edges, bool Truncated);
public sealed record GraphCommunityGroup(int Id, int NodeCount, IReadOnlyList<string> SampleNodeIds);
public sealed record GraphCommunities(int Revision, IReadOnlyList<GraphCommunityGroup> Groups,
    int TotalGroups, int IsolatedNodes, int ScannedNodes, int ScannedEdges, bool Truncated);
public sealed record GraphCrossRepoEdge(string SourceProjectId, string SourceId,
    string TargetProjectId, string TargetId, string OwnerPath, int SourceLine);
public sealed record GraphCrossRepoMap(int Revision, IReadOnlyList<GraphCrossRepoEdge> Edges, bool Truncated, bool Stale = false);
public sealed record GraphEvidenceMatch(string WorkflowNodeId, string EventId, string Reference,
    GraphNode? FileNode);
public sealed record GraphEvidence(IReadOnlyList<GraphEvidenceMatch> Matches, bool Truncated, int Revision);
