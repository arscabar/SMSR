namespace SMSR.CSharpAnalysis;

internal static class ControlOracleSelfCheck
{
    internal static void Run()
    {
        var choices = Enumerable.Range(1,15).Where(mask => System.Numerics.BitOperations.PopCount((uint)mask) <= 2)
            .Select(mask => Enumerable.Range(0,4).Where(n => (mask & (1 << n)) != 0).ToArray()).ToArray();
        var check = SemanticSelfCheck.Require;
        foreach (var a in choices) foreach (var b in choices) foreach (var c in choices)
        {
            var next = new Dictionary<int,int[]> { [0]=a, [1]=b, [2]=c, [3]=[-1], [-1]=[] };
            // Independent oracle: a postdominator blocks every exit path when removed.
            bool ExitWithout(int from, int removed)
            {
                var seen = new HashSet<int> { removed }; var queue = new Queue<int>(); queue.Enqueue(from);
                while (queue.TryDequeue(out var n))
                {
                    if (!seen.Add(n)) continue;
                    if (n == -1) return true;
                    foreach (var target in next[n]) queue.Enqueue(target);
                }
                return false;
            }
            var blocks = Enumerable.Range(0,4).Select(n => new FlowBlock(n,n==3?"Exit":"Block",true,
                next[n].Length==2?"WhenTrue":"None",[],[], n==3?null:new(next[n][0],"Regular",[]),
                next[n].Length==2?new(next[n][1],"Regular",[]):null)).ToArray();
            var budget=4_000_000;
            var actual=ControlFacts.Read(blocks,[],true,"VALUE",ref budget);
            if (Enumerable.Range(0,4).Any(n=>!ExitWithout(n,-2)))
            { check(actual.Status=="UNAVAILABLE","oracle: non-exiting region accepted"); continue; }
            bool Post(int from,int node)=>!ExitWithout(from,node);
            var expected=new HashSet<ControlLink>();
            for(var n=0;n<3;n++) for(var i=0;i<next[n].Length;i++) for(var d=0;d<4;d++)
                if(Post(next[n][i],d) && (d==n || !Post(n,d)))
                    expected.Add(new(n,next[n][i],d,next[n].Length==1?"ALWAYS":i==0?"FALSE":"TRUE"));
            check(expected.SetEquals(actual.Links),"control graph disagrees with path oracle");
            foreach(var p in actual.Postdominators)
            {
                var strict=Enumerable.Range(-1,5).Where(d=>d!=p.Block && Post(p.Block,d)).ToArray();
                check(strict.Single(d=>strict.All(other=>Post(d,other)))==p.Immediate,"immediate postdominator incorrect");
            }
        }
        var emptyBudget=0;
        try { ControlPostdominators.Solve([0],[new(0,-1,"EXIT")],ref emptyBudget); throw new Exception("control limit absent"); }
        catch(ArgumentException e) when(e.Message=="Control dependence work limit exceeded") { }
    }
}
