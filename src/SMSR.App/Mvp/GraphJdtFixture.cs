using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SMSR.App.Mvp;

internal static class GraphJdtFixture
{
    internal static readonly Dictionary<string,string> Sources=new() {
        ["한글.java"]="class 한글 {\r\n static int pick(int x){return x;}\r\n static String pick(String x){return x;}\r\n}\r\n",
        ["Entry.java"]="class Entry {\r\n int number(){/*😀*/return 한글.pick(1);}\r\n String text(){return 한글.pick(\"PRIVATE_JDT_726\");}\r\n int local(){/*😀*/int value=1;return value;}\r\n}\r\n"
    };
    internal static async Task<EventStore> CreateAsync(string root)
    {
        var project=Path.Combine(root,"project");Directory.CreateDirectory(project);
        await File.WriteAllTextAsync(Path.Combine(project,".project"),"<projectDescription><name>SMSRFixture</name><buildSpec/><natures><nature>org.eclipse.jdt.core.javanature</nature></natures></projectDescription>");
        await File.WriteAllTextAsync(Path.Combine(project,".classpath"),"<classpath><classpathentry kind=\"src\" path=\"\"/><classpathentry kind=\"con\" path=\"org.eclipse.jdt.launching.JRE_CONTAINER\"/><classpathentry kind=\"output\" path=\"bin\"/></classpath>");
        var files=new List<GraphFile>();var nodes=new List<GraphNode>();
        foreach(var (path,text) in Sources)
        {
            await File.WriteAllTextAsync(Path.Combine(project,path),text,new UTF8Encoding(false));
            var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
            files.Add(new(path,hash,"code"));nodes.Add(new("file:"+path,path,"file",path,path,1,hash));
        }
        var store=new EventStore(Path.Combine(root,"smsr.db"));await store.InitializeAsync();
        await store.ApplyGraphScanAsync("fixture",new(project,files,nodes,[],[],Sources.Keys.ToArray(),[],[]),false);
        return store;
    }
}
