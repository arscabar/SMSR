namespace SMSR.App.Mvp;
internal static class GraphStructureLinksSelfCheck
{
    internal static void Run()
    {
        var nodes=Enumerable.Range(0,70).Select(i=>new GraphNode("file:"+i+".go",i+".go","code",i+".go",i+".go",1,"hash")).ToArray();
        var edges=(from a in nodes from b in nodes where a!=b select new GraphEdge(a.NodeId,b.NodeId,"CALLS",a.SourcePath,1,"RESOLVED","EXTRACTED")).ToArray();
        var result=GraphStructureBuilder.Build(1,"",nodes,edges);
        if(result.Links.Length!=2000||result.OmittedLinks!=edges.Length-2000||result.TotalFiles!=70)
            throw new Exception("Large relationship payload limit accounting failed");
        try{GraphStructureBuilder.Build(1,"",nodes,[new("missing",nodes[0].NodeId,"CALLS","0.go",1,"RESOLVED","EXTRACTED")]);
            throw new Exception("Missing endpoint accepted");}catch(InvalidOperationException){}
    }
}
