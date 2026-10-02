namespace SMSR.App.Mvp;
public sealed record GraphKnowledgeExport(string ProjectId,int Revision,string Scope,
    IReadOnlyList<GraphNode> Nodes,IReadOnlyList<GraphEdge> Edges,IReadOnlyList<GraphHyperedge> Hyperedges,bool Truncated,GraphOverview? Overview=null);
public sealed class GraphExportService(EventStore store,GraphQueryService query)
{
    public async Task<GraphKnowledgeExport> GetAsync(string projectId,string? nodeId=null,int depth=2,CancellationToken ct=default)
    {
        if(EventValidation.ValidateWorkflowIds(projectId,"export") is{}error)throw new ArgumentException(error);
        var info=await store.GetGraphInfoAsync(projectId,ct)??throw new KeyNotFoundException("색인이 없습니다.");
        if(info.NodeCount>50000||info.EdgeCount>200000)throw new InvalidOperationException("전체 내보내기 한도는 5만 노드·20만 관계입니다.");
        IReadOnlyList<GraphNode> nodes;IReadOnlyList<GraphEdge> edges;var truncated=false;
        if(nodeId is not null)
        {
            var scope=await query.ExportScopeAsync(projectId,nodeId,depth,"both",ct);
            if(scope.Revision!=info.Revision)throw new InvalidOperationException("색인이 변경됐습니다. 다시 조회하세요.");
            nodes=scope.Nodes;edges=scope.Edges;truncated=scope.Truncated;
        }
        else{nodes=await store.GetGraphRevisionNodesAsync(projectId,info.Revision,ct);edges=await store.GetGraphRevisionEdgesAsync(projectId,info.Revision,ct);}
        var ids=nodes.Select(n=>n.NodeId).ToHashSet(StringComparer.Ordinal);
        var groups=new List<GraphHyperedge>();
        for(var offset=0;offset<10000;offset+=100)
        {
            var page=await store.HyperedgeSliceAsync(projectId,info.Revision,null,offset,100,ct);
            groups.AddRange(page.Where(h=>h.Members.All(m=>ids.Contains(m.NodeId))));
            if(page.Length<100)break;if(offset==9900)truncated=true;
        }
        if((await store.GetGraphInfoAsync(projectId,ct))?.Revision!=info.Revision)throw new InvalidOperationException("색인이 변경됐습니다. 다시 조회하세요.");
        GraphOverview? overview=null;
        if(nodeId is null&&await store.GetGraphDerivedAsync(projectId,"overview-v1","",ct)is{}raw)
        {
            var saved=System.Text.Json.JsonSerializer.Deserialize<GraphOverview>(raw,GraphWorker.Json);
            if(saved?.Revision==info.Revision)overview=saved;
        }
        return new(projectId,info.Revision,nodeId??"ALL",nodes.OrderBy(n=>n.NodeId,StringComparer.Ordinal).ToArray(),
            edges.Where(e=>ids.Contains(e.SourceId)&&ids.Contains(e.TargetId)).ToArray(),groups,truncated,overview);
    }
}
