namespace SMSR.CSharpAnalysis;

internal static class DefinitionBoundarySelfCheck
{
    internal static void Run()
    {
        var result = Analyzer.Run(new([new("Bounds.cs", """
            using System;
            class C {
                int Ref(ref int x) => x;
                int Capture(int x) { Func<int> f = () => ++x; f(); return x; }
                int Exception(int x) { try { x++; } finally { x++; } return x; }
                int Deconstruct(int x) { (int a, int b) = (x, 1); return a + b; }
                int Address(int x) { ref int y = ref x; y = 3; return x; }
                int Heap(int x) { var a = new int[1]; a[0] = x; return a[0]; }
                int Out(int x) { int.TryParse("RAW_SECRET", out x); return x; }
            }
            """)]));
        var check = SemanticSelfCheck.Require;
        check(result.Status == "BOUND_INPUT_BUNDLE", "boundary fixture invalid");
        check(result.Functions.All(f => f.Definitions.Status == "UNAVAILABLE" && f.Definitions.Links.Length == 0),
            "unsupported alias/capture/exception claimed precise definitions");
        var invalid = Analyzer.Run(new([new("Bad.cs", "class C { int M(int x) => Missing(x); }")]));
        check(invalid.Functions.All(f => f.Definitions.Status == "UNAVAILABLE"), "invalid code claimed definitions");
        var budget = 0;
        try {
            ReachingDefinitions.Solve([new(0,"Entry",true,"None",[],[],null,null)], [], ref budget);
            throw new Exception("definition work limit missing");
        } catch (ArgumentException e) when (e.Message == "Definition work limit exceeded") { }
        check(!System.Text.Json.JsonSerializer.Serialize(result).Contains("RAW_SECRET"), "literal leaked");
        try {
            var branches = string.Concat(Enumerable.Range(0,150).Select(i => $"if (c) x = {i};"));
            var reads = string.Join("+", Enumerable.Repeat("x",150));
            Analyzer.Run(new([new("Limit.cs", "class C { int M(bool c,int x) { " + branches + "return " + reads + "; } }")]));
            throw new Exception("definition expansion limit missing");
        } catch (ArgumentException e) when (e.Message == "Definition link limit exceeded") { }
    }
}
