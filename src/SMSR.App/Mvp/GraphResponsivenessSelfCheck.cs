using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphResponsivenessSelfCheck
{
    public static async Task RunAsync()
    {
        GraphIndexProbes.SelfCheck();
        var dispatcher=System.Windows.Application.Current.Dispatcher;
        if(!dispatcher.CheckAccess()) throw new Exception("WPF dispatcher thread required");
        var root=Path.Combine(Path.GetTempPath(),"smsr-responsive-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var repo=Path.Combine(root,"repo");await GraphIndexFixture.CreateAsync(repo);
            var data=Path.Combine(root,"data");
            await using var host=await LocalServer.StartAsync(data,0);
            using var client=new HttpClient { BaseAddress=new Uri(host.Address),Timeout=TimeSpan.FromMinutes(3) };
            client.DefaultRequestHeaders.Add("Origin",host.Address);
            var initial=await GraphIndexProbes.RunAsync(client,dispatcher,repo,false);
            var unchanged=await GraphIndexProbes.RunAsync(client,dispatcher,repo,true);
            var health=(await client.GetFromJsonAsync<GraphHealth>("/api/graph/health?projectId=responsive"))!;
            if(health.Info.FileCount!=5000 || health.Info.NodeCount!=50000 || health.Info.EdgeCount!=200000 ||
                health.Info.Revision!=1 || health.DanglingEdges!=0 || health.DuplicateReferences!=0 ||
                health.SelfLoops!=0 || health.IssueCount!=0) throw new Exception("Indexed fixture integrity mismatch");
            await GraphIndexFixture.VerifyAsync(client);
            await File.WriteAllTextAsync(Path.Combine(Path.GetTempPath(),"smsr-p2-index-responsiveness.json"),
                JsonSerializer.Serialize(new {version=2,time=DateTimeOffset.UtcNow,initial,unchanged,
                    health.Info.FileCount,health.Info.NodeCount,health.Info.EdgeCount,
                    databaseBytes=new FileInfo(Path.Combine(data,"smsr.db")).Length,
                    healthMaxTargetMs=2000,dispatcherMaxTargetMs=200,
                    limitation="WPF Input queue only; no window, rendering or real user input exercised"}));
        }
        finally
        {
            var full=Path.GetFullPath(root);var temp=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            if(full.StartsWith(temp,StringComparison.OrdinalIgnoreCase) && Path.GetFileName(full).StartsWith("smsr-responsive-",StringComparison.Ordinal)) Directory.Delete(full,true);
        }
    }
}
