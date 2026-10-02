namespace SMSR.App.Mvp;
internal static class GraphRoleBatchControlSelfCheck
{
    internal static async Task RunAsync(GraphNode node, GraphRoleJobs jobs, GraphRoleBatchService batch)
    {
        await batch.ControlAsync("batch", "pause"); await batch.PumpAsync("batch", default);
        if (await jobs.ReadAsync("batch", node.NodeId) is not null) throw new Exception("Paused batch started");
        await batch.ControlAsync("batch", "resume"); await batch.PumpAsync("batch", default);
        if ((await batch.StatusAsync("batch")).Queued != 1) throw new Exception("Batch did not queue");
        await batch.ControlAsync("batch", "pause");
        if (await batch.CanRunAsync("batch", node.NodeId, default)) throw new Exception("Paused queued job executable");
        await batch.ControlAsync("batch", "resume");
        var job = await jobs.ClaimAsync("batch", node.NodeId);
        await jobs.FinishAsync("batch", node.NodeId, job!.RequestId, null);
        if ((await batch.StatusAsync("batch")).Failed != 1) throw new Exception("Batch failed accounting");
        await batch.PumpAsync("batch", default);
        if ((await jobs.ReadAsync("batch", node.NodeId))?.Status != "FAILED") throw new Exception("Failure retried without consent");
        await batch.ControlAsync("batch", "retry"); await batch.PumpAsync("batch", default);
        if ((await batch.StatusAsync("batch")).Queued != 1) throw new Exception("Batch retry missing");
        await batch.ControlAsync("batch", "pause");
    }
}
