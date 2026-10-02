namespace SMSR.App.Mvp;

public sealed partial class GraphRoleBatchService
{
    internal async Task<bool> CanRunAsync(string projectId, string nodeId, CancellationToken ct)
    {
        var batch = await ReadAsync(projectId, ct);
        return batch is null || !batch.Paused || !batch.Items.Any(i => i.NodeId == nodeId);
    }
    internal async Task PumpAsync(string projectId, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var batch = await ReadAsync(projectId, ct); if (batch is null || batch.Paused) return;
            var records = (await store.GraphRoleJobsAsync(projectId, ct)).ToDictionary(j => j.NodeId);
            if (records.Values.Any(j => j.Status is "QUEUED" or "RUNNING")) return;
            var item = batch.Items.FirstOrDefault(i => i.Error is null && (i.Retry || !records.TryGetValue(i.NodeId, out var j)
                || j.Fingerprint != i.Fingerprint));
            if (item is null) return;
            GraphRoleBatchItem updated;
            try
            {
                var job = await jobs.RequestAsync(projectId, item.NodeId, item.Retry, ct);
                updated = item with { Fingerprint = job.Fingerprint, Retry = false };
                // A reused current role may have no prior job: register it for batch accounting.
                if (job.Status == "SUCCESS") await jobs.RememberAsync(projectId, job, ct);
            }
            catch (Exception e) when (e is ArgumentException or InvalidOperationException or KeyNotFoundException or System.IO.IOException)
            { updated = item with { Error = "원문·색인 근거 확인 실패", Retry = false }; }
            await SaveAsync(projectId, batch with { Items = batch.Items.Select(i => i == item ? updated : i).ToArray() }, ct);
        }
        finally { _gate.Release(); }
    }
    public async Task<GraphRoleBatchStatus> ControlAsync(string projectId, string action, CancellationToken ct = default)
    {
        if (action is not ("pause" or "resume" or "retry")) throw new ArgumentException("일괄 작업 동작이 올바르지 않습니다.");
        await _gate.WaitAsync(ct);
        try
        {
            var batch = await ReadAsync(projectId, ct) ?? throw new KeyNotFoundException("일괄 작업이 없습니다.");
            var records = (await store.GraphRoleJobsAsync(projectId, ct)).ToDictionary(j => j.NodeId);
            if (action == "retry") batch = batch with { Items = batch.Items.Select(i => i.Error is not null
                || records.GetValueOrDefault(i.NodeId)?.Status == "FAILED" ? i with { Error = null, Retry = true, Fingerprint = "" } : i).ToArray() };
            await SaveAsync(projectId, batch with { Paused = action == "pause" }, ct);
            return await StatusAsync(projectId, ct);
        }
        finally { _gate.Release(); }
    }
}
