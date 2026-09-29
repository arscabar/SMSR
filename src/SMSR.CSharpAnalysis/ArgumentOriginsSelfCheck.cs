namespace SMSR.CSharpAnalysis;

internal static class ArgumentOriginsSelfCheck
{
    internal static void Run()
    {
        var r=Analyzer.Run(new([new("Arguments.cs", """
            static class C {
                static int Id(int x)=>x;
                static void Sink(int value) { }
                static void Pair(int left,int right=9) { }
                static void Two(int x,int y) { Sink(Id(x)); Sink(Id(y)); }
                static void Killed(int x) { x=3; Sink(x); }
                static void Mixed(int x) { Sink(x+System.Math.Abs(x)); }
                static void Named(int x,int y) { Pair(right:x,left:y); }
                static void Default(int x) { Pair(x); }
                static void Ref(ref int x) { Sink(x); }
                static int Recur(int x)=>x>0?Recur(x-1):x;
                static void Recursive(int x) { Sink(Recur(x)); }
                static void Loop(int x) { int a=0; while(x>0) { a+=x; x--; } Sink(a); }
            }
            """)]));
        var check=SemanticSelfCheck.Require;
        check(r.Status=="BOUND_INPUT_BUNDLE","argument fixture invalid");
        ArgumentFlow Get(string name)=>r.ReturnSummaries.Single(s=>s.SymbolId.Contains("C."+name+"(")).Arguments;
        ArgumentOrigin Sink(string name)=>Get(name).Origins.Single(o=>o.TargetId!.Contains("C.Sink("));
        var two=Get("Two").Origins.Where(o=>o.TargetId!.Contains("C.Sink(")).ToArray();
        check(two.Length==2 && two[0].Dependencies.Single().ParameterOrdinal==0 &&
            two[1].Dependencies.Single().ParameterOrdinal==1,"void call inputs merged");
        check(two.All(o=>!o.Uncertain && o.Dependencies[0].Path.Any(e=>e.Relation=="ARGUMENT_RETURN_DEPENDENCE")),"nested return not traced");
        check(Sink("Killed").Dependencies.Length==0 && !Sink("Killed").Uncertain,"overwritten input survived");
        foreach(var name in new[]{"Mixed","Recursive"})
            check(Sink(name).Uncertain && Sink(name).Dependencies.Single().ParameterOrdinal==0,"partial input path lost");
        check(Sink("Loop").Dependencies.Single().ParameterOrdinal==0,"loop input lost");
        check(Get("Named").Origins.All(o=>o.Dependencies.Single().ParameterOrdinal==1-o.ParameterOrdinal),"named slots mixed");
        check(Get("Default").Origins.Single(o=>o.ParameterOrdinal==1).Dependencies.Length==0,"default value tainted");
        check(Get("Ref").Status=="UNAVAILABLE","unavailable arguments reported empty success");
        ArgumentPathSelfCheck.Verify(r);
    }
}
