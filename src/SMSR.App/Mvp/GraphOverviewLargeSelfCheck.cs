using System.Diagnostics;
using System.IO;
using System.Text.Json;
namespace SMSR.App.Mvp;
internal static class GraphOverviewLargeSelfCheck
{
    internal static async Task RunAsync()
    {
        var request=new{operation="overview",revision=1,nodes=Enumerable.Range(0,50000).Select(i=>new{nodeId="x"+i,label="Component"+i,kind="symbol",sourcePath="x.py"}).ToArray(),
            edges=Enumerable.Range(0,50000).SelectMany(i=>Enumerable.Range(1,4).Select(step=>new{sourceId="x"+i,targetId="x"+((i+step)%50000),relation="CALLS",resolution="RESOLVED",confidence="EXTRACTED",ownerPath="x.py",sourceLine=1})).ToArray()};
        var watch=Stopwatch.StartNew();var data=await new GraphWorker().RunOverviewAsync(request,default);
        if(data.GetProperty("scannedNodes").GetInt32()!=50000||data.GetProperty("scannedEdges").GetInt32()!=200000)throw new Exception("Bounded large worker result incomplete");
        var members=data.GetProperty("groups").EnumerateArray().SelectMany(g=>g.GetProperty("memberIds").EnumerateArray().Select(n=>n.GetString()!)).ToArray();
        if(members.Length!=50000 || members.Distinct(StringComparer.Ordinal).Count()!=50000 ||
            members.Any(n=>!int.TryParse(n.AsSpan(1),out var id) || n[0]!='x' || id<0 || id>=50000))
            throw new Exception("Large overview membership lost or duplicated");
        if(watch.Elapsed.TotalSeconds>480)throw new Exception("Large overview exceeded existing 8-minute limit");
        await File.WriteAllTextAsync(Path.Combine(Environment.CurrentDirectory,"artifacts/graph-overview-large.json"),JsonSerializer.Serialize(new{
            elapsedSeconds=watch.Elapsed.TotalSeconds,inputBytes=JsonSerializer.SerializeToUtf8Bytes(request,GraphWorker.Json).Length,
            groups=data.GetProperty("groups").GetArrayLength(),nodeCount=50000,edgeCount=200000,membershipCount=members.Length,
            algorithm=data.GetProperty("algorithm").GetString(),workerProcessLimitBytes=1UL*1024*1024*1024,
            limitation="worker enforced process memory limit; not a measured peak-memory value"},GraphWorker.Json));
    }
}
