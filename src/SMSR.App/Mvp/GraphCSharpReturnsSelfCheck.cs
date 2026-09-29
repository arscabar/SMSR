using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphCSharpReturnsSelfCheck
{
    internal static void Verify(JsonElement result)
    {
        var summaries=result.GetProperty("returnSummaries").EnumerateArray().ToArray();
        if(summaries.Length!=3) throw new Exception("Return summaries missing");
        foreach(var summary in summaries)
        {
            var library=summary.GetProperty("source").GetProperty("path").GetString()=="Library.cs";
            var parameters=summary.GetProperty("parameterOrdinals").EnumerateArray().Select(p=>p.GetInt32()).ToArray();
            if(summary.GetProperty("uncertain").GetBoolean() ||
                summary.GetProperty("status").GetString()!="MODELED_RETURN_DEPENDENCE" ||
                !parameters.SequenceEqual([0]))
                throw new Exception("Return input dependencies not persisted");
            if(library) continue;
            var call=summary.GetProperty("calls").EnumerateArray().Single();
            var edge=call.GetProperty("links").EnumerateArray().Single();
            var function=result.GetProperty("functions").EnumerateArray().Single(f=>
                f.GetProperty("symbolId").GetString()==summary.GetProperty("symbolId").GetString());
            var ports=function.GetProperty("definitions").GetProperty("values").GetProperty("ports").EnumerateArray();
            var input=ports.Single(p=>p.GetProperty("kind").GetString()=="CALL_ARGUMENT");
            if(call.GetProperty("status").GetString()!="RETURN_LINKED" || call.GetProperty("uncertain").GetBoolean() ||
                edge.GetProperty("source").GetInt32()!=input.GetProperty("value").GetInt32() ||
                edge.GetProperty("target").GetInt32()!=call.GetProperty("resultValue").GetInt32())
                throw new Exception("Call-local return edge not persisted");
        }
    }
}
