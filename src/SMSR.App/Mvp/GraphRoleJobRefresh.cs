namespace SMSR.App.Mvp;

public sealed partial class GraphRoleJobs
{
    public async Task RefreshAsync(string projectId, CancellationToken ct = default)
    {
        // Previously requested nodes only: indexing is not consent to spend tokens on every file.
        foreach (var job in await store.GraphRoleJobsAsync(projectId, ct))
        {
            try
            {
                var context = await roles.ContextAsync(projectId, job.NodeId, ct);
                if (context.Fingerprint != job.Fingerprint) await RequestAsync(projectId, job.NodeId, ct: ct);
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException) { }
        }
    }
    internal async Task RecoverAsync(string projectId, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            foreach (var job in await store.GraphRoleJobsAsync(projectId, ct))
                if (job.Status == "RUNNING")
                    await SaveAsync(projectId, job with { Status = "QUEUED", UpdatedAt = DateTimeOffset.UtcNow,
                        Revision = (await store.GetGraphInfoAsync(projectId, ct))!.Revision }, ct);
        }
        finally { _gate.Release(); }
    }
}
