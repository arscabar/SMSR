namespace SMSR.App.Mvp;

internal static class GraphBenchmarkOracleSelfCheck
{
    internal static void Run()
    {
        static void Reject(Action action)
        {
            try { action(); } catch(Exception) { return; }
            throw new Exception("Benchmark verifier accepted a corrupt result");
        }
        static GraphEdge Edge(int a,int b)=>new($"n{a:D5}",$"n{b:D5}","REFERENCES","f"+(a/10),1,"RESOLVED","EXPLICIT");
        var direct=new GraphPath([Edge(49999,0)],true,false,2,1);
        GraphBenchmarkOracle.Path(direct,49999,0);
        GraphBenchmarkOracle.Path(new([],true,false,1,1),0,0);
        GraphBenchmarkOracle.Path(new([],false,true,91,1),0,49000);
        if(GraphBenchmarkOracle.Distances(0,5,1)[4105]!=5 || GraphBenchmarkOracle.Distances(0,3,-1).Count!=30)
            throw new Exception("Benchmark distance oracle mismatch");
        Reject(()=>GraphBenchmarkOracle.Path(direct with {Found=false},49999,0));
        Reject(()=>GraphBenchmarkOracle.Path(direct with {Revision=2},49999,0));
        Reject(()=>GraphBenchmarkOracle.Path(direct with {Truncated=true},49999,0));
        Reject(()=>GraphBenchmarkOracle.Path(direct with {Edges=[Edge(49999,1)]},49999,0));
        Reject(()=>GraphBenchmarkOracle.Path(new([Edge(0,1),Edge(1,2)],true,false,3,1),0,2));
        var nodes=new[]{49999,49998,49899,48999}.Select(n=>new GraphNode($"n{n:D5}","f"+(n/10),"code","Node "+n,"f"+(n/10),1,"hash")).ToArray();
        var impact=new GraphImpact(nodes,true,1);GraphBenchmarkOracle.Impact(impact,0,1);
        Reject(()=>GraphBenchmarkOracle.Impact(impact with {Truncated=false},0,1));
        Reject(()=>GraphBenchmarkOracle.Impact(impact with {Revision=2},0,1));
        Reject(()=>GraphBenchmarkOracle.Impact(impact with {Nodes=nodes.Skip(1).ToArray()},0,1));
        Reject(()=>GraphBenchmarkOracle.Impact(impact with {Nodes=[nodes[0],nodes[0],nodes[2],nodes[3]]},0,1));
        Reject(()=>GraphBenchmarkOracle.Impact(impact with {Nodes=[nodes[0] with {Hash="bad"},..nodes.Skip(1)]},0,1));
        if(GraphBenchmarkMetrics.P95(Enumerable.Range(1,40).Select(n=>(double)n).Reverse().ToArray())!=38)
            throw new Exception("Benchmark percentile mismatch");
        GraphBenchmarkMetrics.RequireTarget(2000,2000);
        Reject(()=>GraphBenchmarkMetrics.RequireTarget(2001,1));
        Reject(()=>GraphBenchmarkMetrics.RequireTarget(1,2001));
        Reject(()=>GraphBenchmarkMetrics.RequireTarget(double.NaN,1));
        Reject(()=>GraphBenchmarkMetrics.P95(new double[39]));
        var invalid=new double[40];invalid[0]=double.NaN;
        Reject(()=>GraphBenchmarkMetrics.P95(invalid));
        invalid[0]=-1;Reject(()=>GraphBenchmarkMetrics.P95(invalid));
        var health=new GraphHealth(new("bench","synthetic",1,DateTimeOffset.UnixEpoch,5000,50000,200000,0),0,0,0,0,[]);
        GraphBenchmarkOracle.Health(health);
        Reject(()=>GraphBenchmarkOracle.Health(health with {DanglingEdges=1}));
    }
}
