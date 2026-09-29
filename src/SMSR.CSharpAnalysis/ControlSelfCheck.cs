namespace SMSR.CSharpAnalysis;

internal static class ControlSelfCheck
{
    internal static void Run()
    {
        var result = Analyzer.Run(new([new("Control.cs", """
            using System;
            class C {
                int Diamond(bool c) {
                    int x;
                    if (c) x = 1;
                    else x = 2;
                    return x;
                }
                int Early(bool c) { if (c) return 1; return 2; }
                int Loop(int x) { while (x > 0) x--; return x; }
                int Throw(bool c) { if (c) throw new Exception("SECRET"); return 1; }
                int Dead(bool c) { return 1; if (c) return 2; }
                int Infinite(bool c) { if (c) while (true) {} return 1; }
                int Exceptional(int x) { try { return x; } finally { x++; } }
            }
            """)]));
        var check = SemanticSelfCheck.Require;
        check(result.Status == "BOUND_INPUT_BUNDLE", "control fixture invalid");
        FunctionFlow Get(string name) => result.Functions.Single(f => f.SymbolId.Contains("C." + name + "("));
        var diamond = Get("Diamond");
        check(diamond.Control.Links.Length == 2, "diamond must only control branch arms");
        foreach (var link in diamond.Control.Links)
        {
            var line = diamond.Blocks[link.Dependent].Operations.First().Source.Start.Line;
            check(line == (link.Outcome == "TRUE" ? 4 : 5), "branch truth label reversed");
        }
        check(Get("Early").Control.Links.Length == 2, "early returns not controlled");
        var loop = Get("Loop").Control;
        check(loop.Links.Length == 2 && loop.Links.Any(l => l.Controller == l.Dependent), "loop self dependence missing");
        var thrown = Get("Throw");
        check(thrown.Control.Links.Length == 3 && thrown.Control.Links.Any(l =>
            thrown.Blocks[l.Dependent].Kind == "Exit" && l.Outcome == "FALSE"), "normal versus throw exits lost");
        check(Get("Dead").Control.Links.Length == 0, "unreachable condition included");
        check(Get("Infinite").Control.Limitations.Contains("NON_EXIT_REACHABLE_REGION"), "non-exiting region overclaimed");
        check(Get("Exceptional").Control.Status == "UNAVAILABLE", "exception routing overclaimed");
        check(!System.Text.Json.JsonSerializer.Serialize(result).Contains("SECRET"), "control literal leaked");
        var bad = Analyzer.Run(new([new("Bad.cs", "class C { int M() => Missing(); }")]));
        check(bad.Functions.All(f => f.Control.Status == "UNAVAILABLE"), "invalid control graph claimed verified");
        ControlOracleSelfCheck.Run();
        ControlLimitSelfCheck.Run();
    }
}
