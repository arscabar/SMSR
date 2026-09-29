namespace SMSR.CSharpAnalysis;

internal static class ConnectionLimitSelfCheck
{
    internal static void Run()
    {
        var branches = string.Concat(Enumerable.Range(0, 150).Select(i => $"if (x == {i}) return x;"));
        var calls = string.Concat(Enumerable.Repeat("Pick(x);", 200));
        try
        {
            Analyzer.Run(new([new("Limit.cs", $"class C {{ int Pick(int x) {{ {branches} return 0; }} void Run(int x) {{ {calls} }} }}")]));
            throw new Exception("Connection expansion limit missing");
        }
        catch (ArgumentException e) when (e.Message == "Call connection limit exceeded") { }
    }
}
