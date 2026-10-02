using System.IO;

namespace SMSR.App.Mvp;

internal static class GraphCodeProjectSelfCheck
{
    internal static async Task RunAsync(string root)
    {
        var folder = Path.Combine(Path.GetTempPath(), "smsr-code-project-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var store = new EventStore(Path.Combine(folder, "test.db"));
            await store.InitializeAsync();
            var result = await new GraphIndexService(store).IndexAsync("project-check", root);
            if (result.EdgeCount == 0 || (await store.GetGraphHealthAsync("project-check"))!.DanglingEdges != 0)
                throw new Exception("Project relationships are absent or dangling");
            await GraphProjectLoadSelfCheck.RunAsync(store, root, result);
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(folder, true); }
    }
}
