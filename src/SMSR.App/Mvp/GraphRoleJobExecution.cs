namespace SMSR.App.Mvp;

public sealed partial class GraphRoleJobs
{
    public async Task<GraphRoleJob?> ClaimAsync(string projectId, string nodeId, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var job = await ReadAsync(projectId, nodeId, ct);
            if (job is null || job.Status != "QUEUED") return null;
            var context = await roles.ContextAsync(projectId, nodeId, ct);
            if (context.Fingerprint != job.Fingerprint) throw new InvalidOperationException("설명 요청 근거가 변경되었습니다.");
            job = job with { Status = "RUNNING", Attempts = job.Attempts + 1,
                Revision = context.Revision, UpdatedAt = DateTimeOffset.UtcNow };
            await SaveAsync(projectId, job, ct); return job;
        }
        finally { _gate.Release(); }
    }
    public async Task FinishAsync(string projectId, string nodeId, string requestId,
        GraphRoleRequest? result, CancellationToken ct = default, string? failureCode = null)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var job = await ReadAsync(projectId, nodeId, ct);
            if (job is null || job.RequestId != requestId || (job.Status != "RUNNING" && !(result is null && job.Status == "QUEUED")))
                throw new InvalidOperationException("현재 처리 중인 설명 요청이 아닙니다.");
            if (result is not null)
            {
                if (result.ProjectId != projectId || result.NodeId != nodeId || result.Fingerprint != job.Fingerprint)
                    throw new GraphRoleRuleException("JOB_TARGET");
                await roles.SubmitAsync(result, ct);
            }
            var revision = (await store.GetGraphInfoAsync(projectId, ct))?.Revision
                ?? throw new KeyNotFoundException("프로젝트 색인이 없습니다.");
            await SaveAsync(projectId, job with { Status = result is null ? "FAILED" : "SUCCESS",
                Revision = revision, UpdatedAt = DateTimeOffset.UtcNow,
                Error = result is null ? "설명 생성 실패 · " + (failureCode ?? "연결과 근거를 확인한 후 다시 요청하세요.") : null }, ct);
        }
        finally { _gate.Release(); }
    }
}
