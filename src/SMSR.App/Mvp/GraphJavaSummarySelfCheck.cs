using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphJavaSummarySelfCheck
{
    internal static void Verify(JsonElement result)
    {
        var method=result.GetProperty("methods").EnumerateArray().Single(m=>m.GetProperty("id").GetString()=="source:Entry#run(int)");
        var summary=method.GetProperty("valueSummary");
        var output=summary.GetProperty("returns").EnumerateArray().Single();
        var edge=summary.GetProperty("callEdges").EnumerateArray().Single();
        var connection=result.GetProperty("connections").EnumerateArray().Single();
        var nodes=method.GetProperty("values").GetProperty("nodes").EnumerateArray().ToArray();
        string Kind(string id)=>nodes.Single(n=>n.GetProperty("id").GetString()==id).GetProperty("kind").GetString()!;
        if(summary.GetProperty("status").GetString()!="DATA_DEPENDENCY_CANDIDATES" ||
            output.GetProperty("parameterIndices").GetArrayLength()!=1 || output.GetProperty("parameterIndices")[0].GetInt32()!=0 ||
            output.GetProperty("unknownValueIds").GetArrayLength()!=0 ||
            Kind(edge.GetProperty("source").GetString()!)!="CALL_ARGUMENT_0" || Kind(edge.GetProperty("target").GetString()!)!="OPAQUE_CALL_RESULT" ||
            connection.GetProperty("bodyId").GetString()!="source:Lib#pick(int)" ||
            connection.GetProperty("status").GetString()!="STATIC_BODY_CANDIDATE" ||
            connection.GetProperty("inputs")[0].GetProperty("argumentValueId").GetString()!=edge.GetProperty("source").GetString() ||
            connection.GetProperty("returns")[0].GetProperty("callValueId").GetString()!=edge.GetProperty("target").GetString())
            throw new Exception("Java call-site body/return summary not persisted");
    }
}
