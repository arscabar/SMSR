using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphJdtSelfCheck
{
    internal static async Task RunAsync(string extension)
    {
        var root=Path.Combine(Path.GetTempPath(),"smsr-jdt-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try
        {
            var store=await GraphJdtFixture.CreateAsync(root);var project=Path.Combine(root,"project");
            var rows=new List<object>();var timer=Stopwatch.StartNew();
            foreach(var (line,target,targetLine,name) in new[]{(1,"한글.java",1,"pick"),(2,"한글.java",2,"pick"),(3,"Entry.java",3,"value")})
            {
                var text=GraphJdtFixture.Sources["Entry.java"].Split('\n')[line];
                var position=new GraphLspPosition(line,text.LastIndexOf(name,StringComparison.Ordinal));
                var run=Path.Combine(root,"run"+line);Directory.CreateDirectory(run);
                var started=timer.ElapsedMilliseconds;
                await using var session=GraphJdtProfile.Start(extension,run,project);
                var result=await new GraphLspDefinitions(store).QueryAsync(session,"fixture","Entry.java","java",position,GraphJdtProfile.Options);
                var expected=GraphJdtFixture.Sources[target].Split('\n')[targetLine].IndexOf(name,StringComparison.Ordinal);
                if(result.Excluded!=0 || result.Candidates.Count!=1 || result.Candidates[0].Path!=target ||
                    result.Candidates[0].Range!=new GraphLspRange(new(targetLine,expected),new(targetLine,expected+name.Length)))
                    throw new Exception("Installed JDT definition oracle mismatch: "+JsonSerializer.Serialize(result.Candidates));
                rows.Add(new {line,target,targetLine,range=result.Candidates[0].Range,elapsedMs=timer.ElapsedMilliseconds-started});
            }
            foreach(var (path,text) in GraphJdtFixture.Sources)
                if(await File.ReadAllTextAsync(Path.Combine(project,path))!=text) throw new Exception("JDT changed fixture source");
            if(Directory.EnumerateFiles(project,"*.class",SearchOption.AllDirectories).Any())
                throw new Exception("JDT built fixture bytecode");
            await File.WriteAllTextAsync(Path.Combine(Path.GetTempPath(),"smsr-jdt-self-test.json"),
                JsonSerializer.Serialize(new {version=1,time=DateTimeOffset.UtcNow,extension,elapsedMs=timer.Elapsed.TotalMilliseconds,rows}));
        }
        finally
        {
            var full=Path.GetFullPath(root);var temp=Path.GetFullPath(Path.GetTempPath());
            if(full.StartsWith(temp,StringComparison.OrdinalIgnoreCase) && Path.GetFileName(full).StartsWith("smsr-jdt-",StringComparison.Ordinal)) Directory.Delete(full,true);
        }
    }
}
