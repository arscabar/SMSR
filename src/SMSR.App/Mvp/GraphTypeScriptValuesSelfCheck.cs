using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphTypeScriptValuesSelfCheck
{
    internal static void Verify(JsonElement result)
    {
        var value=result.GetProperty("functions")[0].GetProperty("values");
        var nodes=value.GetProperty("nodes").EnumerateArray().ToArray();
        string Id(string kind)=>nodes.Single(n=>n.GetProperty("kind").GetString()==kind).GetProperty("id").GetString()!;
        var input=Id("PARAMETER_INPUT"); var read=Id("LOCAL_READ"); var output=Id("RETURN");
        var edges=value.GetProperty("edges").EnumerateArray().ToArray();
        bool Edge(string source,string target,string kind)=>edges.Any(e=>e.GetProperty("source").GetString()==source &&
            e.GetProperty("target").GetString()==target && e.GetProperty("relation").GetString()==kind);
        var connection=result.GetProperty("connections")[0];
        if(value.GetProperty("status").GetString()!="VALUE_FLOW_CANDIDATES" || nodes.Length!=3 || edges.Length!=2 ||
            !Edge(input,read,"REACHING_DEFINITION") || !Edge(read,output,"RETURN_VALUE") ||
            connection.GetProperty("inputs")[0].GetProperty("parameterValueId").GetString()!=input ||
            connection.GetProperty("returns")[0].GetProperty("returnValueId").GetString()!=output)
            throw new Exception("TypeScript input-read-return value evidence not persisted");
    }
}
