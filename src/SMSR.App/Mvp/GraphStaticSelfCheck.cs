using System.Diagnostics;
using System.IO;

namespace SMSR.App.Mvp;

internal static class GraphStaticSelfCheck
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "smsr-static-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using (var git = Process.Start(new ProcessStartInfo("git")
            {
                ArgumentList = { "-C", root, "init", "-q" }, UseShellExecute = false, CreateNoWindow = true
            }) ?? throw new InvalidOperationException("Git 실행 실패"))
            {
                await git.WaitForExitAsync();
                if (git.ExitCode != 0) throw new InvalidOperationException("Git 초기화 실패");
            }
            await File.WriteAllTextAsync(Path.Combine(root, "A.cs"), "class A { void Go() { B worker = new B(); worker.Run(); } }");
            await File.WriteAllTextAsync(Path.Combine(root, "B.cs"), "class B { public void Run() {} }");
            var store = new EventStore(Path.Combine(root, "graph.db"));
            await store.InitializeAsync();
            var index = new GraphIndexService(store);
            await index.IndexAsync("static-test", root);
            var query = new GraphQueryService(store);
            var first = await query.ContextAsync("static-test", "file:A.cs");
            if (!first.Outgoing.Any(item => item.Edge.Relation == "CALLS" && item.Node.NodeId == "file:B.cs"))
                throw new InvalidOperationException("C# 파일 간 호출 색인 실패");
            await File.WriteAllTextAsync(Path.Combine(root, "A.cs"), "class A { void Go() {} }");
            await index.IndexAsync("static-test", root);
            if ((await query.ContextAsync("static-test", "file:A.cs")).Outgoing.Any(item => item.Edge.Relation == "CALLS"))
                throw new InvalidOperationException("오래된 호출 관계 제거 실패");
            Directory.CreateDirectory(Path.Combine(root, "other"));
            await File.WriteAllTextAsync(Path.Combine(root, "One.csproj"),
                "<Project><ItemGroup><ProjectReference Include=\"other\\Two.csproj\" /></ItemGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(root, "other", "Two.csproj"), "<Project/>");
            await index.IndexAsync("static-test", root);
            if (!(await query.ContextAsync("static-test", "file:One.csproj")).Outgoing
                .Any(item => item.Edge.Relation == "PROJECT_REFERENCE" && item.Node.NodeId == "file:other/Two.csproj"))
                throw new InvalidOperationException("프로젝트 참조 색인 실패");
        }
        finally
        {
            if (Directory.Exists(root) && root.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase))
                Directory.Delete(root, true);
        }
    }
}
