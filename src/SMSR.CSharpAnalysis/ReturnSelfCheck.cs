namespace SMSR.CSharpAnalysis;

internal static class ReturnSelfCheck
{
    internal static void Run()
    {
        var r = Analyzer.Run(new([new("Returns.cs", """
            static class C {
                static int Id(int x) => x;
                static int First(int x, int y=9) => x;
                static int Two(int x,int y) { var a=Id(x); var b=Id(y); return a; }
                static int Nested(int x) => Id(Id(x));
                static int Named(int x,int y) => First(y:x,x:y);
                static int Default(int x) => First(x);
                static int Recur(int x) => x>0 ? Recur(x-1) : x;
                static int Endless(int x) => Endless(x);
                static int A(int x) => B(x);
                static int B(int x) => x>0 ? A(x-1) : x;
                static int Unknown(int x) => System.Math.Abs(x);
                static int Discard(int x) { Unknown(x); return Id(x); }
                static int Ignored(int x) => First(x,Unknown(x));
                static int Propagate(int x) => Id(Unknown(x));
            }
            """)]));
        var check = SemanticSelfCheck.Require;
        check(r.Status == "BOUND_INPUT_BUNDLE", "return fixture invalid");
        ReturnSummary Get(string name) => r.ReturnSummaries.Single(s => s.SymbolId.Contains("C."+name+"("));
        foreach (var name in new[] { "Id", "First", "Two", "Nested", "Default", "Discard", "Ignored" })
            check(!Get(name).Uncertain && Get(name).ParameterOrdinals.SequenceEqual([0]), name+" return dependencies wrong");
        check(Get("Named").ParameterOrdinals.SequenceEqual([1]), "named argument mapping reversed");
        foreach (var name in new[] { "Recur", "A", "B" })
            check(Get(name).Uncertain && Get(name).ParameterOrdinals.SequenceEqual([0]), "recursive fixed point wrong");
        check(Get("Endless").Uncertain && Get("Endless").ParameterOrdinals.Length==0, "ungrounded recursion proved independent");
        check(Get("Unknown").Uncertain && Get("Propagate").Uncertain, "opaque value lost through a call");
        var two=Get("Two");
        check(two.Calls.Length==2 && two.Calls.All(c=>c.Links.Length==1) &&
            two.Calls[0].Links[0].Source!=two.Calls[1].Links[0].Source &&
            two.Calls.All(c=>c.Links[0].Target==c.ResultValue), "call sites merged");
        try { ReturnSummaries.Read(r.Functions,r.Connections,2); throw new Exception("return budget ignored"); }
        catch (ArgumentException) { }
        foreach(var reversed in ReturnSummaries.Read(r.Functions.Reverse().ToArray(),r.Connections))
        {
            var original=r.ReturnSummaries.Single(s=>s.SymbolId==reversed.SymbolId);
            check(original.Uncertain==reversed.Uncertain && original.ParameterOrdinals.SequenceEqual(reversed.ParameterOrdinals),
                "summary depends on declaration order");
        }
        ReturnBoundarySelfCheck.Run();
    }
}
