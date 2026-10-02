namespace SMSR.App.Mvp;

public sealed record GraphStructureItem(string Id,string Path,string Kind,string Label,
    int Nodes,int Files,GraphNode? Node=null);
public sealed record GraphStructureLink(string SourceId,string TargetId,string Relation,
    string Confidence,string Resolution,int Count,GraphEdge[] Evidence);
public sealed record GraphStructurePage(int Revision,string Path,bool IsFile,
    GraphStructureItem[] Items,GraphStructureItem[] External,GraphStructureLink[] Links,
    int TotalNodes,int TotalFiles,int ContainedEdges,int CrossEdges,int OmittedLinks);
