using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphTypeScriptConnectionSelfCheck
{
    internal static void Verify(JsonElement result)
    {
        var function=result.GetProperty("functions")[0];
        var call=result.GetProperty("calls").EnumerateArray().Single(c=>c.GetProperty("path").GetString()=="Entry.ts");
        var connection=result.GetProperty("connections").EnumerateArray().Single(c=>c.GetProperty("callId").GetString()==call.GetProperty("id").GetString());
        if(connection.GetProperty("status").GetString()!="STATIC_BODY_CANDIDATE" ||
            connection.GetProperty("bodyId").GetString()!=function.GetProperty("id").GetString() ||
            connection.GetProperty("inputs")[0].GetProperty("parameterId").GetString()!=
                function.GetProperty("parameters")[0].GetProperty("id").GetString() ||
            connection.GetProperty("returns")[0].GetProperty("returnId").GetString()!=
                function.GetProperty("returns")[0].GetProperty("id").GetString() ||
            connection.GetProperty("returns")[0].GetProperty("callId").GetString()!=call.GetProperty("id").GetString())
            throw new Exception("TypeScript body input/return connection not persisted");
    }
}
