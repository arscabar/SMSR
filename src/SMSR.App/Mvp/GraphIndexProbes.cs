using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows.Threading;

namespace SMSR.App.Mvp;

internal sealed record GraphProbeStats(int Count,double P95Ms,double MaxMs);
internal sealed record GraphIndexMeasurement(GraphIndexResult Graph,double IndexMs,GraphProbeStats Health,GraphProbeStats Dispatcher);

internal static class GraphIndexProbes
{
    internal static async Task<GraphIndexMeasurement> RunAsync(HttpClient client,Dispatcher dispatcher,string repo,bool unchanged)
    {
        async Task<(GraphIndexResult,double)> Index()
        {
            var watch=Stopwatch.StartNew();
            using var response=await client.PostAsJsonAsync("/api/graph/index",new GraphIndexRequest("responsive",repo)).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var graph=await response.Content.ReadFromJsonAsync<GraphIndexResult>().ConfigureAwait(false);
            return (graph!,watch.Elapsed.TotalMilliseconds);
        }
        var pending=Index();
        var health=Probe(pending,async()=>{using var response=await client.GetAsync("/api/health");response.EnsureSuccessStatusCode();});
        var ui=Probe(pending,()=>dispatcher.InvokeAsync(()=>{},DispatcherPriority.Input).Task.WaitAsync(TimeSpan.FromSeconds(5)));
        await Task.WhenAll(pending,health,ui);
        var (graph,time)=await pending;
        if(graph.FileCount!=5000 || graph.NodeCount!=50000 || graph.EdgeCount!=200000 || graph.Revision!=1 ||
            graph.Unchanged!=unchanged || graph.ChangedFiles!=(unchanged?0:5000) || graph.RemovedFiles!=0)
            throw new Exception("Indexing result mismatch");
        return new(graph,time,Stats(await health,2000),Stats(await ui,200));
    }
    static Task<List<double>> Probe(Task pending,Func<Task> action)=>Task.Run(async()=>
    {
        var samples=new List<double>();
        while(!pending.IsCompleted)
        {
            var timer=Stopwatch.StartNew();await action();samples.Add(timer.Elapsed.TotalMilliseconds);
            await Task.Delay(25);
        }
        return samples;
    });
    static GraphProbeStats Stats(List<double> values,double target)
    {
        if(values.Count<5 || values.Any(v=>!double.IsFinite(v) || v<0) || values.Max()>target)
            throw new Exception($"Indexing response gate failed: count={values.Count}, max={(values.Count==0?double.NaN:values.Max())}, target={target}");
        return new(values.Count,values.Order().ElementAt((int)Math.Ceiling(values.Count*.95)-1),values.Max());
    }
    internal static void SelfCheck()
    {
        if(Stats([5,1,4,2,3],5).P95Ms!=5) throw new Exception("Probe percentile mismatch");
        foreach(var bad in new List<double>[] {[],[1,2,3,4],[1,2,3,4,6],[1,2,3,4,double.NaN]})
        {
            try {Stats(bad,5);} catch(Exception) {continue;}
            throw new Exception("Invalid probe samples accepted");
        }
    }
}
