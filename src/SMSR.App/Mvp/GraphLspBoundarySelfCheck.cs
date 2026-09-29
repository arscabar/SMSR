using System.IO;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphLspBoundarySelfCheck
{
    internal static void Run(string root, string path, string source)
    {
        var span = new GraphLspRange(new(1, 0), new(1, 6));
        var valid = new Uri(Path.Combine(root, path)).AbsoluteUri;
        foreach (var uri in new[] { "https://example.com/file", "file://server/share/file.py",
            new Uri(Path.Combine(Path.GetDirectoryName(root)!, "outside.py")).AbsoluteUri,
            new Uri(Path.Combine(root, "unindexed.py")).AbsoluteUri, valid + "?query=1", valid + "#fragment" })
            if (GraphLspLocations.Parse(JsonSerializer.SerializeToElement(new { uri, range = span }, GraphWorker.Json), root, [path]) is not null)
                throw new Exception("Unsafe definition path accepted");
        foreach (var position in new[] { new GraphLspPosition(-1, 0), new(9, 0), new(0, 5), new(1, 99) })
        {
            try { GraphLspLocations.ValidatePosition(source, position); throw new Exception("Invalid UTF-16 position accepted"); }
            catch (InvalidDataException) { }
        }
        GraphLspLocations.ValidatePosition(source, new(0, 6));
        var reversed = JsonSerializer.SerializeToElement(new { uri = valid, range = new GraphLspRange(new(1, 6), new(1, 0)) }, GraphWorker.Json);
        try { GraphLspLocations.Parse(reversed, root, [path]); throw new Exception("Reversed range accepted"); }
        catch (InvalidDataException) { }
        var missing = JsonSerializer.SerializeToElement(new { uri = valid, range = new { start = new { }, end = new { } } });
        try { GraphLspLocations.Parse(missing, root, [path]); throw new Exception("Missing coordinates accepted"); }
        catch (KeyNotFoundException) { }
    }
}
