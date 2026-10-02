using System.Text.Json;

namespace SMSR.App.Mvp;

public sealed partial class GraphRoleService
{
    public async Task<GraphRoleReport> SubmitAsync(GraphRoleRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = await ContextAsync(request.ProjectId, request.NodeId, ct);
        GraphRoleValidation.Validate(request, context);
        var report = new GraphRoleReport(request.NodeId, context.Fingerprint, context.Revision,
            request.Model, request.Claims, context.Evidence, DateTimeOffset.UtcNow);
        // The host agent interprets semantics; validation proves provenance, not entailment.
        await store.SaveGraphDerivedAsync(request.ProjectId, "node-role", request.NodeId,
            context.Revision, JsonSerializer.Serialize(report, GraphWorker.Json), ct);
        await new GraphVaultService(store, this).DirtyAsync(request.ProjectId, ct);
        return report;
    }

    public async Task<GraphRoleStatus> ReadAsync(string projectId, string nodeId, CancellationToken ct = default)
    {
        var context = await ContextAsync(projectId, nodeId, ct);
        var payload = await store.GetGraphDerivedAsync(projectId, "node-role", nodeId, ct);
        if (payload is null) return new("MISSING", null, "호스트 에이전트의 원문 기반 설명이 아직 없습니다.");
        GraphRoleReport? report;
        try { report = JsonSerializer.Deserialize<GraphRoleReport>(payload, GraphWorker.Json); }
        catch (JsonException) { return new("STALE", null, "설명 저장 형식을 다시 생성해야 합니다."); }
        if (report is not null && report.Contract != "role-v2")
            return new("STALE", null, "설명 계약·근거 정책이 변경되었습니다. 기존 설명을 다시 분석하세요.");
        if (report is null || report.NodeId != nodeId
            || report.Fingerprint != context.Fingerprint)
            return new("STALE", null, "원문 또는 관계가 변경되었습니다. 설명을 다시 분석하세요.");
        return new("CURRENT", report);
    }
}
