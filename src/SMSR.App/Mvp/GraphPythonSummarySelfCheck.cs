using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphPythonSummarySelfCheck
{
    internal static void Verify(JsonElement[] functions)
    {
        JsonElement Summary(string name)=>functions.Single(f=>f.GetProperty("name").GetString()==name).GetProperty("valueSummary");
        int[] Inputs(JsonElement n)=>n.GetProperty("parameterIndices").EnumerateArray().Select(x=>x.GetInt32()).ToArray();
        var slot=Summary("slot").GetProperty("returns")[0];
        if(!Inputs(slot).SequenceEqual(new[]{0}) || slot.GetProperty("unknownValueIds").GetArrayLength()!=0)
            throw new Exception("Python input-return summary missing");
        if(Summary("decision").GetProperty("returns").EnumerateArray().Any(r=>Inputs(r).Length!=0))
            throw new Exception("Python control input leaked into data summary");
        var summary=Summary("summarize");var call=summary.GetProperty("calls")[0];
        var args=call.GetProperty("arguments");var ret=summary.GetProperty("returns")[0];
        if(!Inputs(call.GetProperty("target")).SequenceEqual(new[]{1}) || args.GetArrayLength()!=2 ||
            args.EnumerateArray().Any(a=>!Inputs(a).SequenceEqual(new[]{0})) ||
            args[0].GetProperty("keyword").GetBoolean() || !args[1].GetProperty("keyword").GetBoolean() ||
            Inputs(ret).Length!=0 || ret.GetProperty("unknownValueIds").GetArrayLength()!=1 ||
            ret.GetProperty("unknownValueIds")[0].GetString()!=call.GetProperty("resultValueId").GetString())
            throw new Exception("Python opaque call boundary missing");
        var walk=Summary("walk").GetProperty("calls")[0];
        if(!Inputs(walk.GetProperty("receiver")).SequenceEqual(new[]{1}) ||
            !Inputs(walk.GetProperty("arguments")[0]).SequenceEqual(new[]{0}) ||
            walk.GetProperty("arguments")[0].GetProperty("unknownValueIds").GetArrayLength()==0 ||
            Summary("outer").GetProperty("status").GetString()!="UNAVAILABLE")
            throw new Exception("Python protocol/shared-cell summary boundary missing");
    }
}
