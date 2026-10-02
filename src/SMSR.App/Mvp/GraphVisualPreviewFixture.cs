using System.IO;
namespace SMSR.App.Mvp;
internal static class GraphVisualPreviewFixture
{
    internal static async Task CreateAsync(EventStore store,string root)
    {
        var areas=new[]{"Gateway","Identity","Orders","Payments","Storage","Audit"};
        var files=new List<GraphFile>();var nodes=new List<GraphNode>();var edges=new List<GraphEdge>();
        for(var g=0;g<areas.Length;g++)
        {
            var path=$"src/{areas[g]}.py";var hash=new string('a',64);files.Add(new(path,hash,"code"));
            await File.WriteAllTextAsync(Path.Combine(root,path),"# relation layout test fixture\n");
            for(var i=0;i<6;i++)nodes.Add(new($"demo:{g}:{i}",path,"symbol",$"{areas[g]}.{new[]{"receive","validate","route","process","persist","complete"}[i]}()",path,1,hash));
            for(var i=0;i<6;i++)for(var j=i+1;j<6;j++)edges.Add(new($"demo:{g}:{i}",$"demo:{g}:{j}","CALLS",path,1,"RESOLVED","EXTRACTED"));
            if(g>0)edges.Add(new($"demo:{g-1}:5",$"demo:{g}:0","CALLS",path,1,"RESOLVED","EXTRACTED"));
        }
        edges.Add(new("demo:0:2","demo:5:0","REFERENCES","src/Gateway.py",1,"RESOLVED","INFERRED"));
        await store.ApplyGraphScanAsync("dense-preview",new(root,files,nodes,edges,[],files.Select(f=>f.Path).ToArray(),[],[]),false);
    }
}
