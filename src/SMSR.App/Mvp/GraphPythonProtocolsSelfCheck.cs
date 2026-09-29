using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphPythonProtocolsSelfCheck
{
    internal static void Verify(JsonElement[] functions)
    {
        var model=functions.Single(f=>f.GetProperty("name").GetString()=="walk").GetProperty("values");
        if(model.GetProperty("status").GetString()!="VALUE_FLOW_CANDIDATES") throw new Exception("Python protocol model missing");
        var nodes=model.GetProperty("nodes").EnumerateArray().ToArray();
        var edges=model.GetProperty("edges").EnumerateArray().ToArray();
        string Id(JsonElement n)=>n.GetProperty("id").GetString()!;
        string Kind(JsonElement n)=>n.GetProperty("kind").GetString()!;
        string Input(string name)=>Id(nodes.Single(n=>Kind(n)=="PARAMETER" && n.GetProperty("name").GetString()==name));
        bool Reaches(string start,string end)
        {
            var pending=new Queue<string>(); pending.Enqueue(start); var seen=new HashSet<string>{start};
            while(pending.TryDequeue(out var current))
            {
                if(current==end) return true;
                foreach(var edge in edges.Where(e=>e.GetProperty("source").GetString()==current))
                {
                    var target=edge.GetProperty("target").GetString()!;
                    if(seen.Add(target)) pending.Enqueue(target);
                }
            }
            return false;
        }
        var argument=Id(nodes.Single(n=>Kind(n)=="CALL_ARGUMENT"));
        var receiver=Id(nodes.Single(n=>Kind(n)=="CALL_RECEIVER_CANDIDATE"));
        if(!Reaches(Input("items"),argument) || Reaches(Input("obj"),argument) ||
            !Reaches(Input("obj"),receiver) || Reaches(Input("items"),receiver))
            throw new Exception("Python receiver/argument paths mixed");
        var result=Id(nodes.Single(n=>Kind(n)=="OPAQUE_CALL_RESULT"));
        if(Reaches(Input("items"),result)) throw new Exception("Python opaque call inferred");
        var exit=model.GetProperty("transfers").EnumerateArray().Single(e=>e.GetProperty("kind").GetString()=="ITERATION_EXHAUSTED");
        if(exit.GetProperty("target").GetInt32()==exit.GetProperty("skippedOffset").GetInt32())
            throw new Exception("Python iteration exit did not bypass END_FOR");
    }
}
