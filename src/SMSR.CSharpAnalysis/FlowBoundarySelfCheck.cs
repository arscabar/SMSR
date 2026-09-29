namespace SMSR.CSharpAnalysis;

internal static class FlowBoundarySelfCheck
{
    internal static void Run()
    {
        var check = SemanticSelfCheck.Require;
        var broken = Analyzer.Run(new([new("Error.cs", "class C { int Run() => Unknown(); }")]));
        check(broken.Functions.All(f => f.Status != "COMPILER_CFG" && f.Variables.Status != "COMPILER_REGION_SUMMARY"),
            "invalid program claimed verified flow");
        var text = """
            #line 900 "C:/private/secret.cs"
            class C {
                static int Pick(int n) => n;
                int Run(int 값) { /* 😀 */ return Pick(값); }
            }
            """;
        var output = Analyzer.Run(new([new("한글.cs", text)]));
        foreach (var f in output.Functions)
        {
            var ids = f.Blocks.Select(b => b.Ordinal).ToHashSet();
            foreach (var b in f.Blocks)
            {
                foreach (var edge in new[] { b.FallThrough, b.Conditional })
                    check(edge?.Destination is not { } target || ids.Contains(target), "dangling CFG branch");
                foreach (var group in new[] { b.Operations, b.BranchValue })
                    foreach (var op in group)
                    {
                        check(op.Parent is null || op.Parent < op.Id, "operation parent order invalid");
                        check(op.Source.Path == "한글.cs" && op.Source.Start.Line < 900, "mapped path leaked");
                    }
            }
        }
        var argument = output.Functions.SelectMany(f => f.Blocks).SelectMany(b => b.BranchValue)
            .Single(o => o.Kind == "Argument");
        check(argument.ParameterId == output.Symbols.Single(s => s.Name == "n").Id, "argument parameter identity missing");
        var nested = Analyzer.Run(new([new("Nested.cs", """
            using System;
            class C {
                int A() { int Local(int x) => x; Func<int,int> f = x => x; return Local(f(1)); }
                int B() { int Local(int x) => x; Func<int,int> f = x => x; return Local(f(2)); }
            }
            """)]));
        check(nested.Functions.Length == 6 && nested.Functions.Select(f => f.SymbolId).Distinct().Count() == 6,
            "local/lambda identities collapsed across enclosing methods");
        check(nested.Calls.Where(c => c.Signature?.Contains("Local") == true).Select(c => c.TargetId).Distinct().Count() == 2,
            "local calls linked across lexical scopes");
        try
        {
            Analyzer.Run(new([new("Large.cs", "class C { int Run(int x) { " +
                string.Concat(Enumerable.Repeat("x += 1;", 11000)) + "return x; } }")]));
            throw new Exception("flow limit not enforced");
        }
        catch (ArgumentException e) when (e.Message == "Flow node limit exceeded") { }
    }
}
