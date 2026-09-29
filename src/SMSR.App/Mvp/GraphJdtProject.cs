using System.IO;
using System.Text;
using System.Xml.Linq;

namespace SMSR.App.Mvp;

internal static class GraphJdtProject
{
    internal static async Task<EventStore> CreateAsync(string root,IReadOnlyDictionary<string,string> sources,
        string[] roots,CancellationToken ct)
    {
        var project=Path.Combine(root,"project");Directory.CreateDirectory(project);
        await File.WriteAllTextAsync(Path.Combine(project,".project"),"<projectDescription><name>SMSRSelection</name><buildSpec/><natures><nature>org.eclipse.jdt.core.javanature</nature></natures></projectDescription>",ct);
        var xml=new XElement("classpath",roots.Select(r=>new XElement("classpathentry",new XAttribute("kind","src"),new XAttribute("path",r))),
            new XElement("classpathentry",new XAttribute("kind","con"),new XAttribute("path","org.eclipse.jdt.launching.JRE_CONTAINER")),
            new XElement("classpathentry",new XAttribute("kind","output"),new XAttribute("path",".smsr-output")));
        await File.WriteAllTextAsync(Path.Combine(project,".classpath"),xml.ToString(),ct);
        var files=new List<GraphFile>();var nodes=new List<GraphNode>();
        foreach(var (path,text) in sources)
        {
            var full=Path.Combine(project,path);Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            await File.WriteAllTextAsync(full,text,new UTF8Encoding(false),ct);
            var hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(text)));
            files.Add(new(path,hash,"code"));nodes.Add(new("file:"+path,path,"file",path,path,1,hash));
        }
        var store=new EventStore(Path.Combine(root,"smsr.db"));await store.InitializeAsync(ct);
        await store.ApplyGraphScanAsync("snapshot",new(project,files,nodes,[],[],sources.Keys.ToArray(),[],[]),false,ct);
        return store;
    }
    internal static async Task DeleteAsync(string root)
    {
        var full=Path.GetFullPath(root);var temp=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
        if(!full.StartsWith(temp,StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(full).StartsWith("smsr-jdt-query-",StringComparison.Ordinal))
            throw new InvalidOperationException("Unexpected JDT temporary path");
        // Windows can briefly retain mapped-image handles after process exit.
        // Retry only this owned temporary tree; persistent failures still fail the request.
        for(var attempt=0;;attempt++)
            try { Directory.Delete(full,true); return; }
            catch(Exception error) when(attempt<2 && error is IOException or UnauthorizedAccessException)
            { await Task.Delay(250*(attempt+1)); }
    }
}
