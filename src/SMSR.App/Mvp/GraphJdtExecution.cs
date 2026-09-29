using System.IO;

namespace SMSR.App.Mvp;

internal static class GraphJdtExecution
{
    internal static async Task<(IReadOnlyList<GraphLspDefinition> Candidates,int Excluded)> RunAsync(
        string installation,GraphJdtRequest request,Dictionary<string,string> sources,CancellationToken ct)
    {
        var root=Path.Combine(Path.GetTempPath(),"smsr-jdt-query-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var snapshot=await GraphJdtProject.CreateAsync(root,sources,request.SourceRoots!,ct);
            var project=Path.Combine(root,"project");
            await using var session=GraphJdtProfile.Start(installation,root,project,ct);
            var result=await new GraphLspDefinitions(snapshot).QueryAsync(session,"snapshot",request.Path,"java",
                new(request.Line,request.Character),GraphJdtProfile.Options);
            foreach(var path in request.Paths)
                if(await File.ReadAllTextAsync(Path.Combine(project,path),ct)!=sources[path])
                    throw new InvalidOperationException("분석 도중 복사본이 변경됐습니다.");
            if(Directory.EnumerateFiles(project,"*.class",SearchOption.AllDirectories).Any())
                throw new InvalidOperationException("예상하지 않은 Java 빌드 출력입니다.");
            return result;
        }
        finally { await GraphJdtProject.DeleteAsync(root); }
    }
}
