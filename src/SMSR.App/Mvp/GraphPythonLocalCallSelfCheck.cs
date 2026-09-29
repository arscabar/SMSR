using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphPythonLocalCallSelfCheck
{
    internal static void Verify(JsonElement[] functions)
    {
        JsonElement Function(string name)=>functions.Single(f=>f.GetProperty("name").GetString()==name);
        var outer=Function("local").GetProperty("valueSummary");
        var body=Function("local.<locals>.inner");
        var call=outer.GetProperty("calls")[0];var local=call.GetProperty("localTarget");
        var target=body.GetProperty("valueSummary");
        if(local.GetProperty("status").GetString()!="LOCAL_BODY_CONNECTION_CANDIDATE" ||
            local.GetProperty("bodyIds")[0].GetString()!=body.GetProperty("id").GetString() ||
            local.GetProperty("hasUnknown").GetBoolean() ||
            local.GetProperty("arguments")[0].GetProperty("argumentValueId").GetString()!=call.GetProperty("arguments")[0].GetProperty("valueId").GetString() ||
            local.GetProperty("arguments")[0].GetProperty("parameterValueId").GetString()!=target.GetProperty("parameters")[0].GetProperty("valueId").GetString() ||
            local.GetProperty("returns")[0].GetProperty("returnValueId").GetString()!=target.GetProperty("returns")[0].GetProperty("valueId").GetString() ||
            local.GetProperty("returns")[0].GetProperty("resultValueId").GetString()!=call.GetProperty("resultValueId").GetString())
            throw new Exception("Python local function connection missing");
        if(outer.GetProperty("returns")[0].GetProperty("unknownValueIds").GetArrayLength()!=0 ||
            outer.GetProperty("returns")[0].GetProperty("parameterIndices")[0].GetInt32()!=0 ||
            Function("summarize").GetProperty("valueSummary").GetProperty("calls")[0].GetProperty("localTarget").GetProperty("status").GetString()!="UNRESOLVED")
            throw new Exception("Python unresolved/value propagation boundary lost");
    }
}
