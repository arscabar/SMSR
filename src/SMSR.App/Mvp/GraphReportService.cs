namespace SMSR.App.Mvp;

public sealed record GraphReportRationale(string NodeId,string Label,string Path,int Line,string? Quote);
public sealed record GraphReport(string ProjectId,int Revision,string Status,GraphOverviewPage? Overview,
    GraphCoverage? Coverage,GraphReportRationale[] Rationale,bool Truncated,string[] Limits,int RetryAfterSeconds);

public sealed class GraphReportService(EventStore store,GraphOverviewService overview)
{
    public async Task<GraphReport> GetAsync(string projectId,CancellationToken ct=default)
    {
        var state=await overview.StateAsync(projectId,limit:100,shutdown:ct);
        if(state.Status!="READY")return new(projectId,state.Revision,state.Status,null,null,[],false,
            [state.Error??"그룹 분석이 진행 중입니다."],state.RetryAfterSeconds);
        var revision=state.Revision;
        var coverage=await store.GraphCoverageAsync(projectId,revision,0,100,ct);
        var nodes=await store.GetGraphRevisionNodesAsync(projectId,revision,ct);
        var reasons=nodes.Where(n=>n.Kind=="rationale"||n.Details?.EntityKind=="rationale")
            .OrderBy(n=>n.SourcePath,StringComparer.Ordinal).ThenBy(n=>n.Line).ToArray();
        var current=await store.GetGraphInfoAsync(projectId,ct);
        if(current?.Revision!=revision)throw new InvalidOperationException("분석 중 색인이 변경됐습니다. 다시 조회하세요.");
        return new(projectId,revision,"READY",state.Page,coverage,
            reasons.Take(50).Select(n=>new GraphReportRationale(n.NodeId,n.Label,n.SourcePath,n.Line,n.Details?.Rationale)).ToArray(),
            state.Page!.Truncated||coverage.Truncated||reasons.Length>50,
            ["구조 통계는 역할·실행 순서를 증명하지 않습니다.",
             "설계 이유는 저장된 원문 근거만 포함합니다. 미추출 이유는 추측하지 않습니다.",
             "리비전은 저장 시점을 뜻합니다. 현재 원문 상태는 근거를 열어 확인하세요.",
             "본문·미디어 의미 분석은 명시적으로 제출된 결과만 포함합니다."],0);
    }
}
