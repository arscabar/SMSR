namespace SMSR.CSharpAnalysis;

internal static class FlowMembersSelfCheck
{
    internal static void Run()
    {
        var output = Analyzer.Run(new([new("Members.cs", """
            class A {
                int f;
                public A() { f = 1; }
                ~A() { }
                int P => f;
                int this[int i] => i;
                int Q { get; set; }
                public static A operator +(A a, A b) => a;
            }
            """)]));
        SemanticSelfCheck.Require(output.Status == "BOUND_INPUT_BUNDLE", "member fixture invalid");
        foreach (var id in new[] { "#ctor", "Finalize", "get_P", "get_Item(System.Int32)", "op_Addition(A,A)" })
            SemanticSelfCheck.Require(output.Functions.Single(f => f.SymbolId == "source:M:A." + id).Status == "COMPILER_CFG",
                "explicit member body CFG missing: " + id);
        SemanticSelfCheck.Require(output.Functions.Count(f => f.Status == "NO_CFG") == 2,
            "implicit auto-accessors overclaimed");
    }
}
