using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphPythonCallSummarySelfCheck
{
    internal static void Verify(JsonElement[] functions)
    {
        JsonElement Summary(string name)=>functions.Single(f=>f.GetProperty("name").GetString()==name).GetProperty("valueSummary");
        foreach(var name in new[]{"local","nested","nested.<locals>.middle"})
        {
            var s=Summary(name);var ret=s.GetProperty("returns")[0];
            var call=s.GetProperty("calls")[0];var edges=s.GetProperty("callEdges");
            var result=call.GetProperty("resultSummary");
            if(ret.GetProperty("parameterIndices").GetArrayLength()!=1 || ret.GetProperty("parameterIndices")[0].GetInt32()!=0 ||
                ret.GetProperty("unknownValueIds").GetArrayLength()!=0 || result.GetProperty("parameterIndices")[0].GetInt32()!=0 ||
                edges.GetArrayLength()!=1 || edges[0].GetProperty("source").GetString()!=call.GetProperty("arguments")[0].GetProperty("valueId").GetString() ||
                edges[0].GetProperty("target").GetString()!=call.GetProperty("resultValueId").GetString() ||
                edges[0].GetProperty("bodyId").GetString()!=call.GetProperty("localTarget").GetProperty("bodyIds")[0].GetString())
                throw new Exception("Python composed call data missing: "+name);
        }
        var erased=Summary("erased");
        if(erased.GetProperty("callEdges").GetArrayLength()!=0 ||
            erased.GetProperty("returns")[0].GetProperty("parameterIndices").GetArrayLength()!=0 ||
            erased.GetProperty("returns")[0].GetProperty("unknownValueIds").GetArrayLength()!=0)
            throw new Exception("Python overwritten body input leaked");
    }
}
