using System.IO;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal sealed record GraphLspPosition(int Line, int Character);
internal sealed record GraphLspRange(GraphLspPosition Start, GraphLspPosition End);
internal sealed record GraphLspDefinition(string Path, GraphLspRange Range, string Hash);

internal static class GraphLspLocations
{
    internal static void ValidatePosition(string text, GraphLspPosition position)
    {
        var lines = text.Split('\n');
        if (position.Line < 0 || position.Line >= lines.Length) throw new InvalidDataException("Invalid LSP line");
        var line = lines[position.Line].TrimEnd('\r');
        var column = position.Character;
        if (column < 0 || column > line.Length ||
            (column > 0 && column < line.Length && char.IsHighSurrogate(line[column - 1]) && char.IsLowSurrogate(line[column])))
            throw new InvalidDataException("Invalid LSP UTF-16 column");
    }

    internal static (string Path, GraphLspRange Range)? Parse(JsonElement item, string root, IEnumerable<string> indexedPaths)
    {
        var link = item.TryGetProperty("targetUri", out var target);
        var value = link ? target : item.GetProperty("uri");
        if (!Uri.TryCreate(value.GetString(), UriKind.Absolute, out var uri) || !uri.IsFile ||
            uri.IsUnc || uri.Host.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0) return null;
        var boundary = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var absolute = Path.GetFullPath(uri.LocalPath);
        if (!absolute.StartsWith(boundary, StringComparison.OrdinalIgnoreCase)) return null;
        var relative = Path.GetRelativePath(root, absolute).Replace('\\', '/');
        var matches = indexedPaths.Where(p => p.Equals(relative, StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
        if (matches.Length != 1) return null;
        var data = link ? item.GetProperty("targetSelectionRange") : item.GetProperty("range");
        static GraphLspPosition Position(JsonElement point) =>
            new(point.GetProperty("line").GetInt32(), point.GetProperty("character").GetInt32());
        var range = new GraphLspRange(Position(data.GetProperty("start")), Position(data.GetProperty("end")));
        if (range.Start.Line < 0 || range.Start.Character < 0 ||
            range.End.Line < range.Start.Line || range.End.Character < 0 ||
            (range.End.Line == range.Start.Line && range.End.Character < range.Start.Character))
            throw new InvalidDataException("Invalid LSP range");
        return (matches[0], range);
    }
}
