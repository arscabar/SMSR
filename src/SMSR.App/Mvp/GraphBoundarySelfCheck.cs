using System.Diagnostics;
using System.IO;

namespace SMSR.App.Mvp;

internal static class GraphBoundarySelfCheck
{
    public static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "smsr-boundary-한글-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var start = new ProcessStartInfo("git") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true };
            start.ArgumentList.Add("init"); start.ArgumentList.Add("-q");
            using var git = Process.Start(start)!;
            await git.WaitForExitAsync();
            if (git.ExitCode != 0) throw new InvalidOperationException("경계 시험 Git 준비 실패");
            await File.WriteAllTextAsync(Path.Combine(root, "Widget.cs"), "class Widget {}\n");
            await File.WriteAllTextAsync(Path.Combine(root, "a.md"), "[widget](widget.cs)\n");
            var store = new EventStore(Path.Combine(root, "test.db"));
            await store.InitializeAsync();
            var index = new GraphIndexService(store);
            await index.IndexAsync("case", root);
            await store.RecordAsync(new RecordEventRequest("case-evidence", "case", "work", "node", "agent",
                "NODE_STATUS_CHANGED", "SUCCESS", "done", null, null,
                ["WIDGET.CS", new Uri(Path.Combine(root,"widget.cs")).AbsoluteUri]));
            var evidence = await new GraphEvidenceService(store).GetAsync("case", "work");
            if (evidence.Matches.Count != 2 || evidence.Matches.Any(match => match.FileNode is null))
                throw new Exception("대소문자·file URI 작업 근거 연결 실패");
            File.Move(Path.Combine(root, "Widget.cs"), Path.Combine(root, "renaming.cs"));
            File.Move(Path.Combine(root, "renaming.cs"), Path.Combine(root, "widget.cs"));
            var freshness = await index.CheckFreshnessAsync("case");
            if (freshness.AddedCount != 1 || freshness.RemovedCount != 1) throw new Exception("case-only 이름 변경 감지 실패");
            await index.IndexAsync("case", root);
            var health = await store.GetGraphHealthAsync("case");
            if (health!.DanglingEdges != 0 || await store.GetGraphNodeAsync("case", "file:Widget.cs") is not null
                || await store.GetGraphNodeAsync("case", "file:widget.cs") is null) throw new Exception("case-only 노드 교체 실패");
            var nodes = new[] { "a", "b", "c" }.Select(id => new GraphNode(id,id,"code",id,id,1,"h")).ToArray();
            await store.ApplyGraphScanAsync("depth", new(root, [], nodes,
                [new("a","b","LINKS_TO","a",1,"RESOLVED","EXTRACTED"),new("b","c","LINKS_TO","b",1,"RESOLVED","EXTRACTED")],
                [],["a","b","c"],[],[]), false);
            var query = new GraphQueryService(store);
            var limited = await query.ImpactAsync("depth", "c", 1);
            var complete = await query.ImpactAsync("depth", "c", 2);
            if (!limited.Truncated || limited.Nodes.Count != 1 || complete.Truncated || complete.Nodes.Count != 2)
                throw new Exception("영향 깊이 경계·절단 표시 실패");
            var budget = await query.PathAsync("depth", "a", "b", maxNodes: 1);
            var exact = await query.PathAsync("depth", "a", "b", maxNodes: 2);
            var exhausted = await query.PathAsync("depth", "b", "a", maxDepth: 1);
            if (budget.Found || !budget.Truncated || budget.VisitedNodes != 1
                || !exact.Found || exact.VisitedNodes != 2 || exhausted.Truncated)
                throw new Exception("경로 노드 한도·끝점 절단 검증 실패");
            await GraphUnicodeSelfCheck.RunAsync(store, root, index);
            await GraphRevisionSelfCheck.RunAsync(store, root, index);
            await GraphSensitiveFileSelfCheck.RunAsync(store, root, index);
        }
        finally { Directory.Delete(root, true); }
    }
}
