namespace SMSR.CSharpAnalysis;

internal static class ValueSelfCheck
{
    internal static void Run()
    {
        var result=Analyzer.Run(new([new("Values.cs","""
            class C {
                int Arithmetic(int x) { int a=x+1; int b=a*2; return b; }
                int Kill(int x) { int a=x; a=4; return a; }
                int Post(int x) { int a=x++; return a; }
                int Pre(int x) { int a=++x; return a; }
                int Compound(int x) { x+=(x=5); return x; }
                int Choice(bool c,int x,int y) => c ? x : y;
                int Loop(int x) { int a=0; while(x>0) { a+=x; x--; } return a; }
            }
            """)]));
        var check=SemanticSelfCheck.Require;
        check(result.Status=="BOUND_INPUT_BUNDLE","value fixture invalid");
        ValueFlow Get(string name)=>result.Functions.Single(f=>f.SymbolId.Contains("C."+name+"(")).Definitions.Values;
        int Returned(ValueFlow v)=>v.Ports.Single(p=>p.Kind=="RETURN").Value;
        var arithmetic=Get("Arithmetic");
        check(arithmetic.Status=="LOCAL_VALUE_DEPENDENCE" && Reaches(arithmetic,0,Returned(arithmetic)),"inter-variable arithmetic disconnected");
        var kill=Get("Kill"); check(!Reaches(kill,0,Returned(kill)),"overwritten value survived");
        foreach(var name in new[]{"Post","Pre"})
        {
            var v=Get(name); var increment=v.Nodes.Single(n=>n.Kind=="Increment").Id;
            check(Reaches(v,increment,Returned(v))==(name=="Pre"),"prefix/postfix result confused");
        }
        var compound=Get("Compound"); check(Reaches(compound,0,Returned(compound)),"compound lost old target value");
        var choice=Get("Choice");
        check(!Reaches(choice,0,Returned(choice)) && Reaches(choice,1,Returned(choice)) &&
            Reaches(choice,2,Returned(choice)) && choice.Links.Count(l=>l.Relation=="CAPTURE_DEFINITION")==2,
            "conditional capture merge confused data and control");
        var loop=Get("Loop"); check(Reaches(loop,0,Returned(loop)),"loop carried value disconnected");
        foreach(var f in result.Functions)
        {
            var v=f.Definitions.Values;
            check(v.Links.All(l=>l.Source>=0 && l.Source<v.Nodes.Length && l.Target>=0 && l.Target<v.Nodes.Length),"dangling value edge");
        }
        ValueBoundarySelfCheck.Run();
    }

    internal static bool Reaches(ValueFlow graph,int start,int target)
    {
        var next=graph.Links.GroupBy(l=>l.Source).ToDictionary(g=>g.Key,g=>g.Select(l=>l.Target).ToArray());
        var seen=new HashSet<int>(); var queue=new Queue<int>(); queue.Enqueue(start);
        while(queue.TryDequeue(out var n))
        {
            if(!seen.Add(n)) continue;
            if(n==target) return true;
            foreach(var child in next.GetValueOrDefault(n)??[]) queue.Enqueue(child);
        }
        return false;
    }
}
