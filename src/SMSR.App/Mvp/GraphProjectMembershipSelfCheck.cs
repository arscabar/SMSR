using System.IO;
namespace SMSR.App.Mvp;
internal static class GraphProjectMembershipSelfCheck
{
    internal static async Task RunAsync(string root,GraphIndexService index,EventStore store,GraphQueryService query)
    {
        const string project="graphify-test";
        await File.WriteAllTextAsync(Path.Combine(root,"Demo.csproj"),"<Project><ItemGroup><PackageReference Include=\"Example.Core\" Version=\"1.2.3\" /></ItemGroup></Project>");
        await index.IndexAsync(project,root);
        var before=(await query.ContextAsync(project,"file:Demo.csproj")).Outgoing
            .Single(n=>n.Edge.Relation=="IMPORTS"&&n.Node.Label=="Example.Core (1.2.3)");
        foreach(var adding in new[]{true,false})
        {
            foreach(var name in new[]{"notes.md","image.png"})
            {
                var path=Path.Combine(root,name);
                if(!adding){File.Delete(path);continue;}
                if(name.EndsWith(".md"))await File.WriteAllTextAsync(path,"# Notes\nDemo.csproj\n");
                else await File.WriteAllBytesAsync(path,Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aVr0AAAAASUVORK5CYII="));
            }
            var result=await index.IndexAsync(project,root);
            var after=await store.GetGraphNodeAsync(project,before.Node.NodeId);
            if(result.CodeAnalysis?.Mode!="UNCHANGED"||after?.Details!=before.Node.Details
                ||!(await query.ContextAsync(project,"file:Demo.csproj")).Outgoing.Any(n=>n.Edge==before.Edge))
                throw new Exception("Document/image membership removed project dependency metadata");
        }
    }
}
