using Microsoft.Extensions.Hosting;

namespace SMSR.App.Mvp;

public sealed class GraphRoleJobWorker(EventStore store, GraphRoleJobs jobs, GraphRoleService roles,
    GraphRoleBatchService batches, GraphVaultService vaults) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var project in await store.GetGraphProjectsAsync(stoppingToken))
            await jobs.RecoverAsync(project.ProjectId, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var project in await store.GetGraphProjectsAsync(stoppingToken))
            {
                await batches.PumpAsync(project.ProjectId, stoppingToken);
                foreach (var pending in (await store.GraphRoleJobsAsync(project.ProjectId, stoppingToken)).Where(j => j.Status == "QUEUED"))
                {
                    try
                    {
                        if (!await batches.CanRunAsync(project.ProjectId, pending.NodeId, stoppingToken)) continue;
                        var job = await jobs.ClaimAsync(project.ProjectId, pending.NodeId, stoppingToken);
                        if (job is null) continue;
                        var context = await roles.ContextAsync(project.ProjectId, job.NodeId, stoppingToken);
                        var result = await GraphRoleRunner.RunAsync(project.ProjectId, context, stoppingToken);
                        await jobs.FinishAsync(project.ProjectId, job.NodeId, job.RequestId, result, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                    catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException
                        or System.IO.IOException or System.ComponentModel.Win32Exception or System.Text.Json.JsonException or OperationCanceledException)
                    {
                        var reason = error is GraphRoleRuleException rule ? rule.Message
                            : error is ArgumentException ? "설명 인용·계약 검증 실패 · ARGUMENT"
                            : error is OperationCanceledException ? "5분 실행 제한 초과"
                            : error is InvalidOperationException && error.Message.StartsWith("Codex", StringComparison.Ordinal)
                                ? error.Message : error.GetType().Name;
                        try { await jobs.FinishAsync(project.ProjectId, pending.NodeId, pending.RequestId, null, stoppingToken, reason); }
                        catch (InvalidOperationException) { } // A newer request or index wins; never overwrite it.
                    }
                }
                await vaults.AutoSyncAsync(project.ProjectId, stoppingToken);
            }
            await Task.Delay(1500, stoppingToken);
        }
    }
}
