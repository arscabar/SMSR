namespace SMSR.CSharpAnalysis;

internal static class ConnectionDispatchSelfCheck
{
    internal static void Run()
    {
        var output = Analyzer.Run(new([new("Dispatch.cs", """
            interface I { static virtual int Read() => 1; }
            class D : I { public static int Read() => 2; }
            class C { int Run<T>() where T : I => T.Read(); }
            """)]));
        var check = SemanticSelfCheck.Require;
        check(output.Status == "BOUND_INPUT_BUNDLE", "constrained dispatch fixture invalid");
        check(output.Calls.Single().Dispatch == "CONSTRAINED" && output.Connections.Single().Returns.Length == 0,
            "static virtual constrained dispatch linked to interface body");
        var partial = Analyzer.Run(new([new("Partial.cs", """
            partial class P { private partial int Choose(int x); int Run() => Choose(1); }
            partial class P { private partial int Choose(int x) => x; }
            """)]));
        check(partial.Status == "BOUND_INPUT_BUNDLE", "partial fixture invalid");
        check(partial.Connections.Single().Inputs.Single().ParameterSource!.Start.Line == 1,
            "partial method parameter did not link to implementation");
        var invalid = Analyzer.Run(new([new("Invalid.cs", "class C { int F(int x)=>x; void Run() { F(1); Missing(); } }")]));
        check(invalid.Connections.All(c => c.Status != "BOUNDARY_LINKED"), "invalid compilation boundary marked exact");
    }
}
