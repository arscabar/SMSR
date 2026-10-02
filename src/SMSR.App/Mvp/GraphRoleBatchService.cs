using System.Text.Json;

namespace SMSR.App.Mvp;

public sealed partial class GraphRoleBatchService
{
    internal async Task<GraphRoleBatch?> ReadAsync(string projectId, CancellationToken ct)
        => await store.GetGraphDerivedAsync(projectId, "role-batch", "", ct) is { } raw
            ? JsonSerializer.Deserialize<GraphRoleBatch>(raw, GraphWorker.Json) : null;
    private async Task SaveAsync(string projectId, GraphRoleBatch batch, CancellationToken ct)
        => await store.SaveGraphDerivedAsync(projectId, "role-batch", "",
            (await store.GetGraphInfoAsync(projectId, ct))!.Revision, JsonSerializer.Serialize(batch, GraphWorker.Json), ct);

    public async Task<GraphRoleBatchStatus> StartAsync(GraphRoleBatchRequest request, CancellationToken ct = default)
    {
        if (!request.Confirm) throw new ArgumentException("Codex 사용량 소비와 대상 확인이 필요합니다.");
        await _gate.WaitAsync(ct);
        try
        {
            var old = await StatusAsync(request.ProjectId, ct);
            if (old.Pending + old.Queued + old.Running > 0) throw new InvalidOperationException("기존 일괄 작업을 먼저 마무리하세요.");
            var preview = await PreviewAsync(request.ProjectId, request.Folders, ct);
            if (preview.Revision != request.ExpectedRevision) throw new InvalidOperationException("범위 확인 후 색인이 변경되었습니다.");
            await SaveAsync(request.ProjectId, new(Guid.NewGuid().ToString("N"), preview.Folders,
                preview.Files.Where(f => f.Status is "READY" or "CURRENT").Select(f => new GraphRoleBatchItem(f.NodeId, f.Fingerprint!)).ToArray(),
                false, preview.Current, preview.Excluded), ct);
            return await StatusAsync(request.ProjectId, ct);
        }
        finally { _gate.Release(); }
    }
    public async Task<GraphRoleBatchStatus> StatusAsync(string projectId, CancellationToken ct = default)
    {
        var batch = await ReadAsync(projectId, ct); var counts = new Dictionary<string, int>();
        var records = (await store.GraphRoleJobsAsync(projectId, ct)).ToDictionary(j => j.NodeId);
        foreach (var item in batch?.Items ?? [])
        {
            var status = item.Error is not null ? "FAILED" : records.TryGetValue(item.NodeId, out var job)
                && job.Fingerprint == item.Fingerprint ? job.Status : "PENDING";
            counts[status] = counts.GetValueOrDefault(status) + 1;
        }
        return new(batch, counts.GetValueOrDefault("PENDING"), counts.GetValueOrDefault("QUEUED"),
            counts.GetValueOrDefault("RUNNING"), counts.GetValueOrDefault("SUCCESS"), counts.GetValueOrDefault("FAILED"));
    }
}
