namespace SMSR.App.Mvp;
public sealed record GraphVisualNode(string Id,string Label,int GroupId,string GroupName,
    int Members,int Degree,double? Cohesion,GraphNode? Source);
public sealed record GraphVisualEdge(string Id,string SourceId,string TargetId,string Relation,
    string Confidence,string Resolution,int Count,GraphEdge[] Evidence);
public sealed record GraphVisualHyperedge(string Id,string Label,string[] Nodes);
public sealed record GraphVisualSnapshot(int Revision,string Mode,GraphVisualNode[] Nodes,
    GraphVisualEdge[] Edges,GraphVisualHyperedge[] Hyperedges,int TotalNodes,int TotalEdges,
    int IsolatedNodes,int OmittedNodes,int OmittedEdges,int OmittedHyperedges,string Limitation,
    GraphOverviewCore[]? Core=null,GraphOverviewSurprise[]? Surprises=null,string[]? Questions=null);
