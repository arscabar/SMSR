using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SMSR.App.Services;

internal static partial class CodexMcpConfig
{
    public const string Endpoint = "http://127.0.0.1:49783/mcp";

    public static bool IsRegistered(string path)
    {
        if (!File.Exists(path)) return false;
        var block = GetBlock(File.ReadAllText(path));
        return block is not null
            && block.Contains($"url = {Quote(Endpoint)}", StringComparison.Ordinal)
            && block.Contains("auth = \"oauth\"", StringComparison.Ordinal)
            && block.Contains("startup_timeout_sec = 30", StringComparison.Ordinal)
            && block.Contains("enabled = true", StringComparison.Ordinal);
    }

    public static string? Register(string path)
    {
        var original = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        var newline = original.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var block = $"[mcp_servers.smsr]{newline}url = {Quote(Endpoint)}{newline}auth = \"oauth\"{newline}startup_timeout_sec = 30{newline}enabled = true{newline}";
        var updated = ReplaceBlock(original, block, newline);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var temp = path + ".smsr.tmp";
        File.WriteAllText(temp, updated, new UTF8Encoding(false));
        if (!File.Exists(path))
        {
            File.Move(temp, path);
            return null;
        }

        var backup = path + ".smsr.bak";
        File.Replace(temp, path, backup, true);
        return backup;
    }

    private static string ReplaceBlock(string text, string block, string newline)
    {
        var lines = NewlineRegex().Split(text);
        var start = Array.FindIndex(lines, line => SmsrHeaderRegex().IsMatch(line));
        if (start < 0)
        {
            var gap = text.Length == 0 || text.EndsWith('\n') ? string.Empty : newline;
            return text + gap + (text.Length == 0 ? string.Empty : newline) + block;
        }

        var end = FindEnd(lines, start);
        return string.Concat(lines[..start]) + block + string.Concat(lines[end..]);
    }

    private static string? GetBlock(string text)
    {
        var lines = NewlineRegex().Split(text);
        var start = Array.FindIndex(lines, line => SmsrHeaderRegex().IsMatch(line));
        return start < 0 ? null : string.Concat(lines[start..FindEnd(lines, start)]);
    }

    private static int FindEnd(string[] lines, int start)
    {
        var end = start + 1;
        while (end < lines.Length && (!TableHeaderRegex().IsMatch(lines[end]) || SmsrOwnedHeaderRegex().IsMatch(lines[end]))) end++;
        return end;
    }

    private static string Quote(string value) => $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";

    [GeneratedRegex("(?<=\\n)")]
    private static partial Regex NewlineRegex();
    [GeneratedRegex("^\\s*\\[mcp_servers\\.(?:smsr|\"smsr\")\\]\\s*(?:#.*)?$")]
    private static partial Regex SmsrHeaderRegex();
    [GeneratedRegex("^\\s*\\[")]
    private static partial Regex TableHeaderRegex();
    [GeneratedRegex("^\\s*\\[mcp_servers\\.(?:smsr|\"smsr\")(?:\\.|\\])")]
    private static partial Regex SmsrOwnedHeaderRegex();
}
