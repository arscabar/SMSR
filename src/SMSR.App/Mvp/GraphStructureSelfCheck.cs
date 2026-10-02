namespace SMSR.App.Mvp;
internal static class GraphStructureSelfCheck
{
    internal static void Run()
    {
        GraphNode N(string id,string path,string kind="code")=>new(id,path,kind,id,path,1,new string('a',64));
        var nodes=new[]{N("file:docs/요구 사항.md","docs/요구 사항.md","document"),N("r","docs/요구 사항.md","requirement"),
            N("file:module/alpha/util.py","module/alpha/util.py"),N("a","module/alpha/util.py","symbol"),
            N("file:module/beta/util.ts","module/beta/util.ts"),N("b","module/beta/util.ts","symbol"),
            N("file:assets/photo.png","assets/photo.png","image"),N("file:root.ps1","root.ps1"),N("unknown","","concept")};
        GraphEdge E(string a,string b,string relation="CALLS",string confidence="EXTRACTED")=>new(a,b,relation,"module/alpha/util.py",2,"RESOLVED",confidence);
        var edges=new[]{E("a","b"),E("b","a"),E("a","b","REFERENCES","INFERRED"),E("file:module/alpha/util.py","a","DECLARES")};
        var root=GraphStructureBuilder.Build(7,"",nodes,edges);
        if(root.TotalNodes!=9||root.TotalFiles!=5||root.Items.Sum(i=>i.Nodes)!=9||root.IsFile||root.ContainedEdges!=4)
            throw new Exception("General root hierarchy/unknown accounting failed");
        var scope=GraphStructureBuilder.Build(7,"module/alpha",nodes,edges);
        if(scope.TotalNodes!=2||scope.External.Single().Path!="module/beta"||scope.CrossEdges!=3||scope.Links.Sum(l=>l.Count)!=3
            ||scope.Links.SelectMany(l=>l.Evidence).Any(e=>!edges.Contains(e)))throw new Exception("Boundary/direction/provenance failed");
        var file=GraphStructureBuilder.Build(7,"module/alpha/util.py",nodes,edges);
        if(!file.IsFile||file.Items.Length!=2||file.Links.Length!=4)throw new Exception("File symbols failed");
        var document=new[]{N("file:plan.md","plan.md","document"),N("r","plan.md","requirement"),nodes[3]};
        if(GraphStructureBuilder.Build(7,"module",document,[E("a","r")]).External.Single().Kind!="document")
            throw new Exception("Symbol-only external edge lost original file type");
        if(GraphStructureBuilder.Build(7,"",[],[]).Items.Length!=0)throw new Exception("Empty graph failed");
        foreach(var path in new[]{"../escape","/absolute","C:\\outside"})
            try{GraphStructureBuilder.Build(7,path,nodes,edges);throw new Exception("Unsafe path accepted");}catch(ArgumentException){}
        var large=Enumerable.Range(0,5000).Select(i=>N("file:deep/inner/"+i+".txt","deep/inner/"+i+".txt","document")).ToArray();
        if(GraphStructureBuilder.Build(9,"deep\\inner",large,[]).Items.Length!=5000)throw new Exception("Large/deep/document hierarchy failed");
        GraphStructureLinksSelfCheck.Run();
    }
}
