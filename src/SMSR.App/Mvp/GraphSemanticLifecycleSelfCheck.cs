using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;
namespace SMSR.App.Mvp;
internal static class GraphSemanticLifecycleSelfCheck
{
    internal static async Task RunAsync(string root,EventStore store,GraphIndexService index,GraphDocumentService docs)
    {
        await File.WriteAllTextAsync(Path.Combine(root,"src/other.py"),"def save():\n return False\n");
        await index.IndexAsync("documents",root);
        var symbols=(await store.SearchGraphNodesAsync("documents","save",100,kind:"code")).Where(n=>n.Kind=="symbol").ToArray();
        var same=symbols.GroupBy(n=>n.Label).First(g=>g.Count()>1).ToArray();
        await File.AppendAllTextAsync(Path.Combine(root,"plan.md"),same[0].Label+" is the handler.\n");
        await index.IndexAsync("documents",root);var doc=await docs.ReadAsync("documents","plan.md");
        var request=GraphSemanticFixture.Request(doc);var block=doc.Blocks.Single(b=>b.Text.Contains("is the handler."));
        var ambiguous=new GraphSemanticEdge("events",same[0].NodeId,"REFERENCES",block.Location,block.Text);
        var service=new GraphSemanticService(store,docs);
        try{await service.SubmitAsync(request with {Edges=[ambiguous]});throw new Exception("Ambiguous symbol was confirmed");}catch(ArgumentException){}
        var manifest=new GraphDeepManifest("fixture","preserved",1,"test",null,null,new Dictionary<string,string>(),JsonSerializer.SerializeToElement(new{}),JsonSerializer.SerializeToElement(new{}));
        await using(var db=new SqliteConnection("Data Source="+Path.Combine(root,"smsr.db")))
        {await db.OpenAsync();using var sql=GraphSql.Create(db,null,"INSERT INTO graph_deep_manifests VALUES($p0,$p1,$p2,$p3,$p4)","documents",doc.Revision,"fixture","preserved",JsonSerializer.Serialize(manifest,GraphWorker.Json));await sql.ExecuteNonQueryAsync();}
        var saved=await service.SubmitAsync(request with{Edges=[..request.Edges,ambiguous with{Inferred=true}]});
        if((await store.GetGraphDeepManifestAsync("documents",saved.Revision,"fixture","preserved"))?.Hash!=manifest.Hash)
            throw new Exception("Semantic revision lost immutable deep provenance");
        await File.AppendAllTextAsync(Path.Combine(root,"src/service.py"),"# live drift\n");
        if(!(await service.ReadAsync("documents","plan.md")).Stale)throw new Exception("Live dependency drift invisible");
        await index.IndexAsync("documents",root);
        if(!(await service.ReadAsync("documents","plan.md")).Stale)throw new Exception("Changed dependency cache reused");
    }
}
