using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphCSharpArgumentsSelfCheck
{
    internal static void Verify(JsonElement result)
    {
        foreach(var summary in result.GetProperty("returnSummaries").EnumerateArray())
        {
            var arguments=summary.GetProperty("arguments");
            if(arguments.GetProperty("status").GetString()!="MODELED_ARGUMENT_ORIGINS")
                throw new Exception("Argument status not persisted");
            var origins=arguments.GetProperty("origins").EnumerateArray().ToArray();
            if(summary.GetProperty("source").GetProperty("path").GetString()=="Library.cs")
            {
                if(origins.Length!=0) throw new Exception("Spurious argument origins");
                continue;
            }
            var origin=origins.Single(); var dependency=origin.GetProperty("dependencies").EnumerateArray().Single();
            var path=dependency.GetProperty("path").EnumerateArray().ToArray();
            var id=dependency.GetProperty("entryValue").GetInt32();
            if(origin.GetProperty("uncertain").GetBoolean() || origin.GetProperty("parameterOrdinal").GetInt32()!=0 ||
                dependency.GetProperty("parameterOrdinal").GetInt32()!=0 || path.Length!=2 ||
                origin.GetProperty("callStatus").GetString()!="BOUNDARY_LINKED")
                throw new Exception("Actual argument witness missing");
            foreach(var edge in path)
            {
                if(edge.GetProperty("source").GetInt32()!=id) throw new Exception("Broken stored argument path");
                id=edge.GetProperty("target").GetInt32();
            }
            if(id!=origin.GetProperty("value").GetInt32()) throw new Exception("Stored argument endpoint mismatch");
        }
    }
}
