using System.Text.Json;

namespace SMSR.App.Mvp;

public sealed record GraphRoleJob(string RequestId, string NodeId, string Fingerprint, int Revision,
    string Status, int Attempts, DateTimeOffset UpdatedAt, string? Error = null);
public sealed record GraphRoleJobRequest(string ProjectId, string NodeId, bool Retry = false);

public sealed partial class GraphRoleJobs(EventStore store, GraphRoleService roles)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public async Task<GraphRoleJob> RequestAsync(string projectId, string nodeId, bool retry = false,
        CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var context = await roles.ContextAsync(projectId, nodeId, ct);
            if (context.Node.Kind is not ("code" or "symbol")) throw new ArgumentException("코드 역할 설명만 자동 요청할 수 있습니다.");
            var job = await ReadAsync(projectId, nodeId, ct);
            if ((await roles.ReadAsync(projectId, nodeId, ct)).Status == "CURRENT" && !retry)
                return new(job?.RequestId ?? "", nodeId, context.Fingerprint, context.Revision, "SUCCESS", 0, DateTimeOffset.UtcNow);
            if (job?.Fingerprint == context.Fingerprint && job.Status is "QUEUED" or "RUNNING"
                && DateTimeOffset.UtcNow - job.UpdatedAt < TimeSpan.FromMinutes(10)) return job;
            if (job?.Fingerprint == context.Fingerprint && job.Status == "FAILED" && !retry) return job;
            job = new(Guid.NewGuid().ToString("N"), nodeId, context.Fingerprint, context.Revision,
                "QUEUED", 0, DateTimeOffset.UtcNow);
            await SaveAsync(projectId, job, ct); return job;
        }
        finally { _gate.Release(); }
    }
    public async Task<GraphRoleJob?> ReadAsync(string projectId, string nodeId, CancellationToken ct = default)
    {
        if (EventValidation.ValidateWorkflowIds(projectId, "role") is { } error
            || string.IsNullOrWhiteSpace(nodeId) || nodeId.Length > 1024)
            throw new ArgumentException("설명 요청 식별자가 올바르지 않습니다.");
        var json = await store.GetGraphDerivedAsync(projectId, "node-role-job", nodeId, ct);
        return json is null ? null : JsonSerializer.Deserialize<GraphRoleJob>(json, GraphWorker.Json);
    }
    private Task SaveAsync(string projectId, GraphRoleJob job, CancellationToken ct)
        => store.SaveGraphDerivedAsync(projectId, "node-role-job", job.NodeId, job.Revision,
            JsonSerializer.Serialize(job, GraphWorker.Json), ct);
    internal Task RememberAsync(string projectId, GraphRoleJob job, CancellationToken ct) => SaveAsync(projectId, job, ct);
}
