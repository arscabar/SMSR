namespace SMSR.App.Mvp;

public sealed record GraphOverviewRepresentative(string NodeId, string Label, int Degree);
public sealed record GraphOverviewGroup(int Id, string Name, int NodeCount, double Cohesion,
    string[] MemberIds, GraphOverviewRepresentative[] Representatives);
public sealed record GraphOverviewLink(int SourceGroup, int TargetGroup, string Relation, string Confidence, string Resolution, int Count);
public sealed record GraphOverviewCore(string NodeId, string Label, int Degree, string Reason);
public sealed record GraphOverviewSurprise(GraphEdge Edge, string Reason, string Question);
public sealed record GraphOverview(int Revision, string Algorithm, GraphOverviewGroup[] Groups,
    GraphOverviewLink[] Links, GraphOverviewCore[] Core, GraphOverviewSurprise[] Surprises, string[] Questions,
    int IsolatedNodes, int ScannedNodes, int ScannedEdges, bool LinksTruncated, string Limitation);
public sealed record GraphOverviewGroupPage(int Id, string Name, int NodeCount, double Cohesion,
    GraphOverviewRepresentative[] Representatives,string[]? Paths=null);
public sealed record GraphOverviewPage(int Revision, string Algorithm, GraphOverviewGroupPage[] Groups,
    int TotalGroups, GraphOverviewLink[] Links, GraphOverviewCore[] Core, GraphOverviewSurprise[] Surprises,
    string[] Questions, int IsolatedNodes, int ScannedNodes, int ScannedEdges, bool Truncated, string Limitation);
public sealed record GraphOverviewMembers(int Revision, int GroupId, int Total, GraphNode[] Nodes, bool Truncated);
public sealed record GraphOverviewState(string Status,int Revision,GraphOverviewPage? Page,int RetryAfterSeconds,string? Error=null);
