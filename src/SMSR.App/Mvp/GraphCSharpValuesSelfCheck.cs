using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphCSharpValuesSelfCheck
{
    internal static void Verify(JsonElement result)
    {
        foreach(var function in result.GetProperty("functions").EnumerateArray())
        {
            var graph=function.GetProperty("definitions").GetProperty("values");
            var nodes=graph.GetProperty("nodes"); var links=graph.GetProperty("links").EnumerateArray().ToArray();
            foreach(var link in links)
                if(link.GetProperty("source").GetInt32()<0 || link.GetProperty("source").GetInt32()>=nodes.GetArrayLength() ||
                    link.GetProperty("target").GetInt32()<0 || link.GetProperty("target").GetInt32()>=nodes.GetArrayLength())
                    throw new Exception("C# dangling stored value link");
            var ports=graph.GetProperty("ports").EnumerateArray().ToArray();
            var returned=ports.Single(p=>p.GetProperty("kind").GetString()=="RETURN").GetProperty("value").GetInt32();
            var reached=new HashSet<int>{0}; var changed=true;
            while(changed) { changed=false; foreach(var link in links)
                if(reached.Contains(link.GetProperty("source").GetInt32())) changed|=reached.Add(link.GetProperty("target").GetInt32()); }
            if(function.GetProperty("source").GetProperty("path").GetString()=="Library.cs")
            {
                if(graph.GetProperty("status").GetString()!="LOCAL_VALUE_DEPENDENCE" || !reached.Contains(returned))
                    throw new Exception("C# input-to-return value chain missing");
            }
            else
            {
                var output=ports.Single(p=>p.GetProperty("kind").GetString()=="CALL_RESULT").GetProperty("value").GetInt32();
                if(graph.GetProperty("status").GetString()!="PARTIAL_VALUE_DEPENDENCE" ||
                    nodes[output].GetProperty("status").GetString()!="OPAQUE_CALL" || links.Any(l=>l.GetProperty("target").GetInt32()==output))
                    throw new Exception("C# opaque call result silently inferred");
            }
        }
    }
}
