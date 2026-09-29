using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;

namespace SMSR.App.Mvp;

internal static class GraphPerformanceSelfCheck
{
    public static async Task RunAsync()
    {
        GraphBenchmarkOracleSelfCheck.Run();
        var root=Path.Combine(Path.GetTempPath(),"smsr-graph-bench-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var database=Path.Combine(root,"smsr.db");
            await GraphBenchmarkFixture.CreateAsync(database);
            await using var host=await LocalServer.StartAsync(root,0);
            using var client=new HttpClient { BaseAddress=new Uri(host.Address),Timeout=TimeSpan.FromSeconds(30) };
            GraphBenchmarkOracle.Health((await client.GetFromJsonAsync<GraphHealth>("/api/graph/health?projectId=bench"))!);
            var path=new List<double>();var impact=new List<double>();var lengths=new Dictionary<int,int>();
            async Task<T> Read<T>(string url,List<double> times,bool sample)
            {
                var timer=Stopwatch.StartNew();var result=await client.GetFromJsonAsync<T>(url);
                if(sample) times.Add(timer.Elapsed.TotalMilliseconds);
                return result??throw new Exception("Missing benchmark response");
            }
            int[] deltas=[0,1,1001,1102,2103,3104,4105,49000];
            for(var i=0;i<45;i++)
            {
                var start=i*997%50000;var end=(start+deltas[i%8])%50000;var depth=i%5+1;
                var result=await Read<GraphPath>($"/api/graph/path?projectId=bench&fromId=n{start:D5}&toId=n{end:D5}&maxDepth=5",path,i>=5);
                var distance=GraphBenchmarkOracle.Path(result,start,end);
                if(i>=5) lengths[distance]=lengths.GetValueOrDefault(distance)+1;
                var affected=await Read<GraphImpact>($"/api/graph/impact?projectId=bench&nodeId=n{start:D5}&maxDepth={depth}",impact,i>=5);
                GraphBenchmarkOracle.Impact(affected,start,depth);
            }
            GraphBenchmarkOracle.Health((await client.GetFromJsonAsync<GraphHealth>("/api/graph/health?projectId=bench"))!);
            await GraphBenchmarkMetrics.SaveAsync(database,path,impact,lengths);
        }
        finally
        {
            var full=Path.GetFullPath(root);var temp=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            if(full.StartsWith(temp,StringComparison.OrdinalIgnoreCase) && Path.GetFileName(full).StartsWith("smsr-graph-bench-",StringComparison.Ordinal)) Directory.Delete(full,true);
        }
    }
}
