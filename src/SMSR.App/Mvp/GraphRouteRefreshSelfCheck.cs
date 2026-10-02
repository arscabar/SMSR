using System.IO;

namespace SMSR.App.Mvp;

internal static class GraphRouteRefreshSelfCheck
{
    internal static async Task RunAsync(string root, GraphIndexService index, EventStore store)
    {
        const string project = "graphify-test";
        await File.WriteAllTextAsync(Path.Combine(root, "routes.cs"),
            "class Routes { [McpServerTool(Name=\"sample_tool\")] void Map() { app.MapGet(\"/ready\", () => 1); } }");
        await index.IndexAsync(project, root);
        var before = (await store.SearchGraphNodesAsync(project, "", kind: "api_route"))
            .Concat(await store.SearchGraphNodesAsync(project, "", kind: "mcp_tool")).ToArray();
        if (before.Length != 2) throw new Exception("Route fixture missing");
        await File.WriteAllTextAsync(Path.Combine(root, "a.py"), "def changed():\n return 1\n");
        await index.IndexAsync(project, root);
        var context = await new GraphQueryService(store).ContextAsync(project, "file:routes.cs");
        foreach (var route in before)
            if ((await store.GetGraphNodeAsync(project, route.NodeId)) is null
                || !context.Outgoing.Any(n => n.Node.NodeId == route.NodeId && n.Edge.Relation == "DECLARES"))
                throw new Exception("Unchanged API/MCP declaration lost during code refresh");
        if (!(await index.IndexAsync(project, root)).Unchanged) throw new Exception("Route refresh revised unchanged graph");
    }
}
