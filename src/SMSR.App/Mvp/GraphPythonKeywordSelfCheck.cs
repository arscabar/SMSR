using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphPythonKeywordSelfCheck
{
    internal const string Source="\ndef keyword(value,unused):\n def inner(first,second): return second\n return inner(second=value,first=unused)\n"+
        "\ndef keyword_only(value,unused):\n def inner(first,/,*,second): return second\n return inner(unused,second=value)\n"+
        "\ndef keyword_invalid(value):\n def inner(item): return item\n return inner(PRIVATE_KEYWORD_5283=value)\n";

    internal static void Verify(JsonElement[] functions)
    {
        JsonElement Summary(string name)=>functions.Single(f=>f.GetProperty("name").GetString()==name).GetProperty("valueSummary");
        foreach(var name in new[]{"keyword","keyword_only"})
        {
            var s=Summary(name);var call=s.GetProperty("calls")[0];var target=call.GetProperty("localTarget");
            var ret=s.GetProperty("returns")[0];var edge=s.GetProperty("callEdges")[0];
            if(target.GetProperty("status").GetString()!="LOCAL_BODY_CONNECTION_CANDIDATE" ||
                ret.GetProperty("parameterIndices")[0].GetInt32()!=0 || ret.GetProperty("unknownValueIds").GetArrayLength()!=0 ||
                edge.GetProperty("parameterIndex").GetInt32()!=1 ||
                edge.GetProperty("source").GetString()!=call.GetProperty("arguments")[name=="keyword"?0:1].GetProperty("valueId").GetString())
                throw new Exception("Python keyword data binding missing");
        }
        var bindings=Summary("keyword").GetProperty("calls")[0].GetProperty("localTarget").GetProperty("arguments");
        if(bindings[0].GetProperty("parameterIndex").GetInt32()!=1 || bindings[1].GetProperty("parameterIndex").GetInt32()!=0)
            throw new Exception("Python keyword order confused with parameter order");
        var bad=Summary("keyword_invalid");var local=bad.GetProperty("calls")[0].GetProperty("localTarget");
        if(local.GetProperty("bindingReason").GetString()!="UNKNOWN_OR_POSITIONAL_ONLY_KEYWORD" ||
            bad.GetProperty("callEdges").GetArrayLength()!=0 || local.GetProperty("arguments").GetArrayLength()!=0 ||
            functions.Any(f=>f.GetRawText().Contains("PRIVATE_KEYWORD_5283")))
            throw new Exception("Python invalid keyword/privacy guard missing");
    }
}
