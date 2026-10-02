namespace SMSR.App.Mvp;

public sealed partial class GraphOverviewService
{
    public async Task<GraphStructurePage> StructureAsync(string projectId,string? path=null,
        int? expectedRevision=null,CancellationToken ct=default)
    {
        if(EventValidation.ValidateWorkflowIds(projectId,"graph") is{}error)throw new ArgumentException(error);
        var info=await store.GetGraphInfoAsync(projectId,ct)??throw new KeyNotFoundException("먼저 프로젝트를 색인하세요.");
        if(expectedRevision.HasValue&&expectedRevision!=info.Revision)
            throw new InvalidOperationException("색인이 갱신되었습니다. 구조를 다시 확인하세요.");
        if(info.NodeCount>50000||info.EdgeCount>200000)
            throw new InvalidOperationException("구조도 한도는5만 노드·20만 관계입니다. 색인 범위를 줄이세요.");
        var nodes=await store.GetGraphRevisionNodesAsync(projectId,info.Revision,ct);
        var edges=await store.GetGraphRevisionEdgesAsync(projectId,info.Revision,ct);
        return GraphStructureBuilder.Build(info.Revision,path??"",nodes.ToArray(),edges.ToArray());
    }
}
