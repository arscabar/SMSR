using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SMSR.App.Mvp;

internal sealed record GraphRouteDeclaration(string Kind, string Method, string Value, int Line);

internal static class GraphRouteExtractor
{
    private static readonly Regex AspNet = new(@"\.Map(?<method>Get|Post|Put|Delete|Patch)\s*\(\s*\""(?<value>/[^\""\r\n]{0,256})\""",
        RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    private static readonly Regex Web = new(@"(?:@(?:app|router)\.|\b(?:app|router)\.)(?<method>get|post|put|delete|patch)\s*\(\s*['\""`](?<value>/[^'\""`\r\n]{0,256})['\""`]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
    private static readonly Regex Mcp = new(@"\[McpServerTool\s*\(\s*Name\s*=\s*\""(?<value>[A-Za-z0-9_\-]{1,128})\""",
        RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    private static readonly Regex Sensitive = new(@"(?i)(smds_|sk-[a-z0-9]{10}|(?:api[_-]?key|token|secret|password)\s*[=:]|Bearer\s+|eyJ[a-z0-9_-]{10,}\.)",
        RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    public static IReadOnlyList<GraphRouteDeclaration> Extract(string path, byte[] bytes)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension is not (".cs" or ".py" or ".js" or ".ts")) return [];
        var result = new List<GraphRouteDeclaration>();
        var source = Encoding.UTF8.GetString(bytes);
        var code = GraphCodeMask.Create(source, extension == ".py");
        var lineStarts = new List<int> { 0 };
        for (var i = 0; i < source.Length; i++) if (source[i] == '\n') lineStarts.Add(i + 1);
        void Add(Regex pattern, bool tool)
        {
            foreach (Match match in pattern.Matches(source))
            {
                var value = match.Groups["value"].Value;
                if (!code[match.Index] || Sensitive.IsMatch(value) || value.Contains("${") || value.Contains('\\')) continue;
                var position = lineStarts.BinarySearch(match.Index);
                var line = position >= 0 ? position + 1 : ~position;
                result.Add(new(tool ? "mcp_tool" : "api_route", tool ? "MCP" : match.Groups["method"].Value.ToUpperInvariant(), value, line));
            }
        }
        Add(extension == ".cs" ? AspNet : Web, false);
        if (extension == ".cs") Add(Mcp, true);
        return result.OrderBy(item => item.Line).ToArray();
    }
}
