using System.Text.Json;

namespace SMSR.CSharpAnalysis;

internal static class SemanticSelfCheck
{
    internal static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    internal static void Run()
    {
        var output = Analyzer.Run(new([
            new("Library.cs", """
                namespace Alpha;
                public class Helper {
                    public static int Pick(int x) => x;
                    public static string Pick(string x) => x;
                    public static T Identity<T>(T x) => x;
                    public static int Pair(int left, int right = 2) => left + right;
                    public virtual int Virtual() => 1;
                }
                public class Derived : Helper { public override int Virtual() => 2; }
                public static class Extensions { public static int Twice(this int x) => x * 2; }
                """),
            new("한글/Entry.cs", """
                using Alias = Alpha.Helper;
                using Alpha;
                using System;
                static class Entry {
                    public static int Run() {
                        var x = Alias.Pick(1);
                        var y = Alias.Pick("secret string not emitted");
                        var g = Alias.Identity(3);
                        var e = (2).Twice();
                        var p = Alias.Pair(right: 4, left: 2);
                        var optional = Alias.Pair(1);
                        Helper h = new Derived(); var v = h.Virtual();
                        Func<int, int> d = Alias.Pick; var indirect = d(2);
                        int Pick(int z) => z;
                        return Pick(x);
                    }
                }
                """) ]));
        Require(output.Status == "BOUND_INPUT_BUNDLE", JsonSerializer.Serialize(output.Diagnostics));
        var calls = output.Calls;
        Require(calls.Any(c => c.TargetId == "source:M:Alpha.Helper.Pick(System.Int32)"), "int overload missing");
        Require(calls.Any(c => c.TargetId == "source:M:Alpha.Helper.Pick(System.String)"), "string overload missing");
        Require(calls.Any(c => c.Signature == "Alpha.Helper.Identity<int>(int)"), "generic inference missing");
        Require(calls.Any(c => c.TargetId == "source:M:Alpha.Extensions.Twice(System.Int32)"), "extension target missing");
        Require(calls.Any(c => c.Arguments.Select(a => a.Ordinal).SequenceEqual(new[] { 1, 0 })), "named argument binding missing");
        Require(calls.Any(c => c.Arguments.Any(a => a.Kind == "DefaultValue")), "default argument binding missing");
        Require(calls.Count(c => c.Resolution == "STATIC_TARGET_ONLY") == 2, "virtual/delegate overclaimed");
        Require(calls.Last().TargetId == output.Symbols.Single(s => s.Name == "Pick" && s.Source?.Path == "한글/Entry.cs").Id, "shadowed local method linked by name");
        Require(calls.Any(c => c.Signature == "Alpha.Derived.Derived()"), "constructor target missing");
        Require(output.References.Any(r => r.Alias == "Alias" && r.TargetId == "source:T:Alpha.Helper"), "type alias missing");
        Require(!JsonSerializer.Serialize(output).Contains("secret string"), "source literal leaked");
        SemanticNegativeSelfCheck.Run();
    }
}
