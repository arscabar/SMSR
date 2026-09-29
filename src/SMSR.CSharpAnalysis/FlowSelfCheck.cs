using System.Text.Json;

namespace SMSR.CSharpAnalysis;

internal static class FlowSelfCheck
{
    internal static void Run()
    {
        var result = Analyzer.Run(new([new("Flow.cs", """
            using System;
            abstract class Flow {
                public abstract int Missing(int p);
                public int Run(bool choose, int input) {
                    int value;
                    if (choose) value = input; else value = 0;
                    while (value < 3) value++;
                    return value;
                }
                public int Nested(int input) {
                    int Local(int x) => x + input;
                    Func<int,int> f = x => x * input;
                    return Local(f(input));
                }
                public int Exceptional(int x) {
                    try { if (x == 0) throw new Exception("RAW_LITERAL_NOT_SAVED"); return x; }
                    catch (Exception) { return -1; }
                    finally { x++; }
                }
                public int Dead() { return 1; int unused = 2; }
                public int Prop { get { return 2; } }
            }
            """)]));
        var check = SemanticSelfCheck.Require;
        check(result.Status == "BOUND_INPUT_BUNDLE", JsonSerializer.Serialize(result.Diagnostics));
        var run = result.Functions.Single(f => f.SymbolId == "source:M:Flow.Run(System.Boolean,System.Int32)");
        check(run.Status == "COMPILER_CFG" && run.Blocks.Any(b => b.Condition != "None"), "CFG branches missing");
        check(run.Blocks.Any(b => b.FallThrough?.Destination <= b.Ordinal || b.Conditional?.Destination <= b.Ordinal), "loop back edge missing");
        var value = result.Symbols.Single(s => s.Name == "value").Id;
        check(run.Variables.Status == "COMPILER_REGION_SUMMARY" && run.Variables.Read.Contains(value) &&
            run.Variables.Written.Contains(value) && run.Variables.AlwaysAssigned.Contains(value), "variable flow summary missing");
        check(result.Functions.Any(f => f.SymbolId == result.Symbols.Single(s => s.Name == "Local").Id && f.Blocks.Length > 0), "local function CFG missing");
        check(result.Functions.Any(f => f.SymbolId.EndsWith(":Method") && f.Variables.Captured.Length > 0), "lambda CFG/capture missing");
        var exceptional = result.Functions.Single(f => f.SymbolId == "source:M:Flow.Exceptional(System.Int32)");
        check(exceptional.Regions.Any(r => r.Kind == "Catch") && exceptional.Regions.Any(r => r.Kind == "Finally"), "exception regions missing");
        check(exceptional.Blocks.Any(b => b.FallThrough?.FinallyRegions.Length > 0), "finally routing missing");
        check(result.Functions.Any(f => f.Status == "NO_CFG"), "abstract method overclaimed");
        check(result.Functions.SelectMany(f => f.Blocks).Any(b => !b.Reachable), "unreachable block missing");
        check(!JsonSerializer.Serialize(result).Contains("RAW_LITERAL_NOT_SAVED"), "operation literal leaked");
        FlowBoundarySelfCheck.Run();
        FlowMembersSelfCheck.Run();
    }
}
