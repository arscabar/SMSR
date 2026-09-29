using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphTypeScriptSummarySelfCheck
{
    internal static void Verify(JsonElement result)
    {
        var function=result.GetProperty("functions").EnumerateArray().Single(f=>f.GetProperty("path").GetString()=="Wrapper.ts");
        var summary=function.GetProperty("valueSummary");
        var output=summary.GetProperty("returns").EnumerateArray().Single();
        var edge=summary.GetProperty("callEdges").EnumerateArray().Single();
        var nodes=function.GetProperty("values").GetProperty("nodes").EnumerateArray().ToArray();
        string Kind(string id)=>nodes.Single(n=>n.GetProperty("id").GetString()==id).GetProperty("kind").GetString()!;
        if(summary.GetProperty("status").GetString()!="DATA_DEPENDENCY_CANDIDATES" ||
            output.GetProperty("parameterIndices").GetArrayLength()!=1 || output.GetProperty("parameterIndices")[0].GetInt32()!=0 ||
            output.GetProperty("unknownValueIds").GetArrayLength()!=0 ||
            Kind(edge.GetProperty("source").GetString()!)!="CALL_ARGUMENT_0" ||
            Kind(edge.GetProperty("target").GetString()!)!="OPAQUE_CALL_RESULT" ||
            edge.GetProperty("relation").GetString()!="BODY_DATA_DEPENDENCY_CANDIDATE")
            throw new Exception("Call-site data dependency summary not persisted");
    }
}
