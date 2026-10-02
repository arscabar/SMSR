namespace SMSR.App.Mvp;
internal static class GraphRoleBatchRefreshSelfCheck
{
    internal static async Task RunAsync(EventStore store, string root, GraphNode node, GraphRoleService roles,
        GraphRoleJobs jobs, GraphRoleBatchService batch)
    {
        var active = await jobs.ClaimAsync("batch", node.NodeId);
        var context = await roles.ContextAsync("batch", node.NodeId);
        await jobs.FinishAsync("batch", node.NodeId, active!.RequestId,
            new("batch", node.NodeId, context.Revision, context.Fingerprint, "test",
                [new("ROLE", "True를 반환하는 예제", "EXTRACTED", ["e0"], "return True")]));
        var path = node.SourcePath;
        await store.ApplyGraphScanAsync("batch", new(root, [new(path, node.Hash, "code")], [node],
            [new(node.NodeId, node.NodeId, "REFERENCES", path, 1, "RESOLVED", "EXTRACTED")], [], [path], [], []), false);
        await jobs.RefreshAsync("batch");
        if ((await jobs.ReadAsync("batch", node.NodeId))?.Status != "QUEUED") throw new Exception("Batch changed role not requeued");
        await batch.ControlAsync("batch", "resume");
        var updated = await jobs.ClaimAsync("batch", node.NodeId);
        await jobs.FinishAsync("batch", node.NodeId, updated!.RequestId, null);
        await batch.PumpAsync("batch", default);
        if ((await batch.StatusAsync("batch")).Failed != 1) throw new Exception("Batch changed fingerprint accounting");
        await batch.ControlAsync("batch", "pause");
    }
}
