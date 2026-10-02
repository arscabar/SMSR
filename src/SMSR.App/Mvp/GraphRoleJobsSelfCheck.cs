namespace SMSR.App.Mvp;

internal static class GraphRoleJobsSelfCheck
{
    internal static async Task RunAsync(EventStore store, GraphRoleService roles, GraphRoleContext context)
    {
        var jobs = new GraphRoleJobs(store, roles);
        var first = await jobs.RequestAsync("roles", context.Node.NodeId, true);
        var second = await jobs.RequestAsync("roles", context.Node.NodeId, true);
        if (first.RequestId != second.RequestId || first.Status != "QUEUED"
            || (await new GraphRoleJobs(store, roles).ReadAsync("roles", context.Node.NodeId))?.RequestId != first.RequestId)
            throw new Exception("Role request duplicate/persistence failure");
        var claims = await Task.WhenAll(jobs.ClaimAsync("roles", first.NodeId), jobs.ClaimAsync("roles", first.NodeId));
        if (claims.Count(j => j is not null) != 1) throw new Exception("Role lease double claimed");
        try { await jobs.FinishAsync("roles", first.NodeId, "other", GraphRoleSelfCheck.Request(context)); throw new Exception("Wrong request accepted"); }
        catch (InvalidOperationException) { }
        await jobs.FinishAsync("roles", first.NodeId, first.RequestId, GraphRoleSelfCheck.Request(context));
        if ((await jobs.ReadAsync("roles", first.NodeId))?.Status != "SUCCESS") throw new Exception("Role completion failure");
        var retry = await jobs.RequestAsync("roles", first.NodeId, true); await jobs.ClaimAsync("roles", first.NodeId);
        await jobs.FinishAsync("roles", first.NodeId, retry.RequestId, null);
        if ((await jobs.RequestAsync("roles", first.NodeId, true)).Status != "QUEUED") throw new Exception("Role explicit retry failed");
        await jobs.RecoverAsync("roles", default);
        var last = await jobs.ReadAsync("roles", first.NodeId);
        await jobs.FinishAsync("roles", first.NodeId, last!.RequestId, null);
    }
}
