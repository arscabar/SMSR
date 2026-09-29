using System.Text.Json;

namespace SMSR.CSharpAnalysis;

internal static class SemanticNegativeSelfCheck
{
    internal static void Run()
    {
        var uncertain = Analyzer.Run(new([new("Bad.cs", """
            class C {
                static void Pick(string value) { }
                static void Pick(System.Text.StringBuilder value) { }
                void Run(dynamic d) { Pick(null); Missing("DO_NOT_LEAK"); d.Call(); }
            }
            """)]));
        SemanticSelfCheck.Require(uncertain.Status == "COMPILATION_ERRORS", "error status missing");
        SemanticSelfCheck.Require(uncertain.Calls.Any(c => c.Resolution == "AMBIGUOUS" && c.Candidates.Length == 2), "ambiguous overload forced");
        SemanticSelfCheck.Require(uncertain.Calls.Any(c => c.Dispatch == "DYNAMIC" && c.TargetId is null), "dynamic target guessed");
        SemanticSelfCheck.Require(uncertain.Calls.All(c => c.Resolution != "BOUND_IN_BUNDLE"), "invalid bundle marked exact");
        SemanticSelfCheck.Require(!JsonSerializer.Serialize(uncertain).Contains("DO_NOT_LEAK"), "diagnostic source leaked");
        var condition = new SourceInput("Conditional.cs", """
            #line 200 "C:/outside/private.cs"
            class C {
            #if FEATURE
                static int Target(int x) => x;
            #else
                static string Target(string x) => x;
            #endif
                static object Run() => Target(1);
            }
            """);
        var enabled = Analyzer.Run(new([condition], Defines: ["FEATURE"]));
        var disabled = Analyzer.Run(new([condition]));
        SemanticSelfCheck.Require(enabled.Status == "BOUND_INPUT_BUNDLE" && disabled.Status == "COMPILATION_ERRORS", "defines ignored");
        SemanticSelfCheck.Require(enabled.Calls.Single().Source.Path == "Conditional.cs" && enabled.Calls.Single().Source.Start.Line < 200,
            "line directive escaped source evidence");
        foreach (var invalid in new[] { "../escape.cs", "C:/escape.cs", "/escape.cs", "a\\b.cs", "a//b.cs" })
            try { CompilationInput.Create(new([new(invalid, "class C {}") ])); throw new Exception("Invalid path accepted"); }
            catch (ArgumentException) { }
        try { CompilationInput.Create(new([new("A.cs", ""), new("a.cs", "") ])); throw new Exception("Duplicate path accepted"); }
        catch (ArgumentException) { }
    }
}
