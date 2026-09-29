using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphCSharpConnectionsSelfCheck
{
    internal static void Verify(JsonElement result)
    {
        GraphCSharpValuesSelfCheck.Verify(result);
        GraphCSharpReturnsSelfCheck.Verify(result);
        GraphCSharpArgumentsSelfCheck.Verify(result);
        foreach (var f in result.GetProperty("functions").EnumerateArray().Where(f =>
            f.GetProperty("source").GetProperty("path").GetString() == "Library.cs"))
        {
            var definitions = f.GetProperty("definitions");
            var hasBranch = f.GetProperty("symbolId").GetString() == "source:M:Lib.Pick(System.Int32)";
            var control = f.GetProperty("control");
            if (control.GetProperty("status").GetString() != "NORMAL_CFG_CONTROL_DEPENDENCE" ||
                control.GetProperty("links").GetArrayLength() != (hasBranch ? 2 : 0) ||
                control.GetProperty("postdominators").GetArrayLength() != f.GetProperty("blocks").GetArrayLength())
                throw new Exception("C# branch control dependence not persisted");
            if (definitions.GetProperty("status").GetString() != "MAY_REACHING_DEFINITIONS" ||
                definitions.GetProperty("links").GetArrayLength() != (hasBranch ? 3 : 1) ||
                definitions.GetProperty("sites")[0].GetProperty("kind").GetString() != "ENTRY")
                throw new Exception("C# parameter-to-read definitions not persisted");
        }
        var link = result.GetProperty("connections").EnumerateArray().Single();
        var input = link.GetProperty("inputs").EnumerateArray().Single();
        var output = link.GetProperty("returns").EnumerateArray().Single();
        if (link.GetProperty("status").GetString() != "BOUNDARY_LINKED" ||
            input.GetProperty("parameterSource").GetProperty("path").GetString() != "Library.cs" ||
            input.GetProperty("argument").GetProperty("valueSource").GetProperty("path").GetString() != "Entry.cs" ||
            output.GetProperty("source").GetProperty("path").GetString() != "Library.cs" ||
            output.GetProperty("callSource").GetProperty("path").GetString() != "Entry.cs")
            throw new Exception("C# cross-file input/output evidence missing");
    }
}
