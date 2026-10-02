namespace SMSR.App.Mvp;
internal static class GraphOverviewScaleFixture
{
    internal static async Task CreateAsync(EventStore store,string root)
    {
        var nodes=Enumerable.Range(0,5150).Select(i=>new GraphNode("scale"+i,"src/service.py","symbol",
            "시험 구성요소 "+i,"src/service.py",1,"fixture")).ToArray();
        var edges=new List<GraphEdge>();
        for(var start=0;start<150;start+=5)for(var i=start;i<start+5;i++)for(var j=i+1;j<start+5;j++)
            edges.Add(new("scale"+i,"scale"+j,"CALLS","src/service.py",1,"RESOLVED","EXTRACTED"));
        await store.ApplyGraphScanAsync("scale-preview",new(root,[new("src/service.py","fixture","code")],nodes,edges,[],["src/service.py"],[],[]),false);
    }
}
