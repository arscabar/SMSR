namespace SMSR.App.Mvp;

// Independent arithmetic oracle for the synthetic ring, not production traversal code.
internal static class GraphBenchmarkOracle
{
    internal static Dictionary<int,int> Distances(int start,int depth,int direction)
    {
        var distances=new Dictionary<int,int>{{start,0}};var queue=new Queue<int>();queue.Enqueue(start);
        while(queue.TryDequeue(out var node))
        {
            var d=distances[node];if(d==depth) continue;
            foreach(var step in new[]{1,2,101,1001})
            {
                var next=(node+direction*step+50000)%50000;
                if(distances.TryAdd(next,d+1)) queue.Enqueue(next);
            }
        }
        return distances;
    }
    internal static void Health(GraphHealth h)
    {
        if(h.Info.ProjectId!="bench" || h.Info.Revision!=1 || h.Info.FileCount!=5000 || h.Info.NodeCount!=50000 ||
            h.Info.EdgeCount!=200000 || h.DanglingEdges!=0 || h.DuplicateReferences!=0 || h.SelfLoops!=0 || h.IssueCount!=0)
            throw new Exception("Benchmark fixture mismatch");
    }
    internal static int Path(GraphPath path,int start,int end)
    {
        var distance=Distances(start,5,1).GetValueOrDefault(end,-1);
        if(path.Revision!=1 || path.Found!=(distance>=0) || path.Truncated!=(distance<0) ||
            path.Edges.Count!=Math.Max(0,distance) || path.VisitedNodes is <1 or >1000)
            throw new Exception("Benchmark shortest-path mismatch");
        var at=$"n{start:D5}";
        foreach(var e in path.Edges)
        {
            var a=Id(e.SourceId);var b=Id(e.TargetId);
            if(e.SourceId!=at || !new[]{1,2,101,1001}.Contains((b-a+50000)%50000) ||
                e.Relation!="REFERENCES" || e.OwnerPath!="f"+(a/10) || e.SourceLine!=1 ||
                e.Resolution!="RESOLVED" || e.Confidence!="EXPLICIT") throw new Exception("Benchmark edge mismatch");
            at=e.TargetId;
        }
        if(path.Found && at!=$"n{end:D5}") throw new Exception("Benchmark endpoint mismatch");
        return distance;
    }
    internal static void Impact(GraphImpact impact,int start,int depth)
    {
        var expected=Distances(start,depth,-1).Keys.Where(n=>n!=start).Select(n=>$"n{n:D5}").ToHashSet();
        if(impact.Revision!=1 || !impact.Truncated || impact.Nodes.Count!=expected.Count ||
            !expected.SetEquals(impact.Nodes.Select(n=>n.NodeId))) throw new Exception("Benchmark impact mismatch");
        foreach(var n in impact.Nodes)
        {
            var id=Id(n.NodeId);
            if(n.OwnerPath!="f"+(id/10) || n.SourcePath!=n.OwnerPath || n.Kind!="code" || n.Label!="Node "+id || n.Line!=1 || n.Hash!="hash")
                throw new Exception("Benchmark node mismatch");
        }
    }
    static int Id(string s)=>s.Length==6 && s[0]=='n' && int.TryParse(s.AsSpan(1),out var n) && n is >=0 and <50000?n:throw new Exception("Benchmark node ID mismatch");
}
