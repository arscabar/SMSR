using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;

namespace SMSR.App.Mvp;

internal static class GraphIndexFixture
{
    internal static async Task CreateAsync(string repo)
    {
        Directory.CreateDirectory(repo);
        var git=new ProcessStartInfo("git") {WorkingDirectory=repo,UseShellExecute=false,CreateNoWindow=true};
        git.ArgumentList.Add("init");git.ArgumentList.Add("-q");
        using var process=Process.Start(git)!;await process.WaitForExitAsync();
        if(process.ExitCode!=0) throw new Exception("Git fixture setup failed");
        for(var i=0;i<5000;i++)
        {
            var text=new StringBuilder();var link=0;
            for(var h=0;h<9;h++)
            {
                text.AppendLine($"# Heading {i}-{h}");
                for(var j=0;j<(h==8?7:3);j++)
                    text.AppendLine($"[reference](file{(i+ ++link)%5000}.md)");
            }
            await File.WriteAllTextAsync(Path.Combine(repo,$"file{i}.md"),text.ToString());
        }
    }
    internal static async Task VerifyAsync(HttpClient client)
    {
        async Task<GraphContext> Context(string id)=>(await client.GetFromJsonAsync<GraphContext>(
            "/api/graph/context?projectId=responsive&limit=100&nodeId="+Uri.EscapeDataString(id)))!;
        foreach(var file in new[]{0,4999})
        {
            var path=$"file{file}.md";var context=await Context("file:"+path);
            var owners=Enumerable.Range(1,31).Select(i=>$"file{(file-i+5000)%5000}.md").ToHashSet();
            if(context.Revision!=1 || context.Truncated || context.Outgoing.Count!=9 || context.Incoming.Count!=31 ||
                !owners.SetEquals(context.Incoming.Select(n=>n.Edge.OwnerPath))) throw new Exception("Indexed file relationships mismatch");
            foreach(var heading in context.Outgoing)
            {
                var h=int.Parse(heading.Node.Label.Split('-')[1]);var detail=await Context(heading.Node.NodeId);
                var targets=Enumerable.Range(h*3+1,h==8?7:3).Select(i=>$"file:file{(file+i)%5000}.md").ToHashSet();
                if(h is <0 or >8 || heading.Edge.Relation!="CONTAINS" || heading.Edge.SourceLine!=h*4+1 ||
                    detail.Truncated || detail.Revision!=1 || detail.Outgoing.Count!=targets.Count ||
                    !targets.SetEquals(detail.Outgoing.Select(n=>n.Node.NodeId)) ||
                    detail.Outgoing.Any(n=>n.Edge.Relation!="LINKS_TO" || n.Edge.OwnerPath!=path || n.Edge.Resolution!="RESOLVED"))
                    throw new Exception("Indexed heading targets mismatch");
            }
        }
    }
}
