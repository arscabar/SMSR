using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphRoleVersionSelfCheck
{
    internal static async Task RunAsync(EventStore store, GraphRoleService roles, GraphRoleContext context)
    {
        var report = (await roles.ReadAsync("roles", context.Node.NodeId)).Report!;
        await store.SaveGraphDerivedAsync("roles", "node-role", context.Node.NodeId, context.Revision,
            JsonSerializer.Serialize(report with { Contract = "role-v1" }, GraphWorker.Json), default);
        if ((await roles.ReadAsync("roles", context.Node.NodeId)).Status != "STALE"
            || await new GraphRoleJobs(store, roles).ReadAsync("roles", context.Node.NodeId) is not null)
            throw new Exception("Old explanation accepted or contract migration spent tokens");
        await roles.SubmitAsync(GraphRoleSelfCheck.Request(context));
    }
}
