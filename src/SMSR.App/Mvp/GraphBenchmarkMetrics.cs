using System.IO;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphBenchmarkMetrics
{
    internal static double P95(IReadOnlyList<double> samples)
    {
        if(samples.Count!=40 || samples.Any(v=>!double.IsFinite(v) || v<0)) throw new Exception("Invalid benchmark samples");
        return samples.Order().ElementAt(37);
    }
    internal static void RequireTarget(double path,double impact)
    {
        if(!double.IsFinite(path) || !double.IsFinite(impact) || path<0 || impact<0 || path>2000 || impact>2000)
            throw new Exception("Graph benchmark p95 exceeded 2000 ms");
    }
    internal static async Task SaveAsync(string database,List<double> path,List<double> impact,Dictionary<int,int> lengths)
    {
        var p=P95(path);var i=P95(impact);
        var report=new { version=2,time=DateTimeOffset.UtcNow,files=5000,nodes=50000,edges=200000,
            transport="loopback HTTP with JSON decoding",topology="directed ring: +1,+2,+101,+1001 modulo 50000",
            databaseBytes=new FileInfo(database).Length,warmupPerKind=5,samples=40,
            pathP95Ms=p,impactP95Ms=i,pathMaxMs=path.Max(),impactMaxMs=impact.Max(),pathLengths=lengths,
            impactDepths=new[]{1,2,3,4,5},correctness="independent shortest paths and complete bounded reverse sets",
            targetP95Ms=2000,sqliteMeetsTarget=p<=2000 && i<=2000 };
        await File.WriteAllTextAsync(Path.Combine(Path.GetTempPath(),"smsr-graph-benchmark.json"),JsonSerializer.Serialize(report));
        RequireTarget(p,i);
    }
}
