using System.IO;
using System.Text;
using System.Text.Json;

namespace SMSR.App.Services;

internal sealed record CodexTokenUsage(long Input, long Output, string RolloutPath);

internal static class CodexTokenUsageReader
{
    private const int TailBytes = 4 * 1024 * 1024;

    public static CodexTokenUsage? Read(string sessionId, string? knownPath = null,
        string? sessionsRoot = null, string? hookPath = null)
    {
        sessionsRoot ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex", "sessions");
        var path = Valid(knownPath, sessionId, sessionsRoot) ?? Valid(hookPath, sessionId, sessionsRoot)
            ?? Find(sessionId, sessionsRoot);
        if (path is null) return null;
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var length = (int)Math.Min(stream.Length, TailBytes);
            stream.Seek(-length, SeekOrigin.End);
            var buffer = new byte[length];
            stream.ReadExactly(buffer);
            foreach (var line in Encoding.UTF8.GetString(buffer).Split('\n').Reverse())
            {
                if (!line.Contains("\"type\":\"token_count\"", StringComparison.Ordinal)) continue;
                using var json = JsonDocument.Parse(line);
                var usage = json.RootElement.GetProperty("payload").GetProperty("info").GetProperty("total_token_usage");
                var input = usage.GetProperty("input_tokens").GetInt64();
                var output = usage.GetProperty("output_tokens").GetInt64();
                if (input >= 0 && output >= 0) return new(input, output, path);
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException) { }
        return null;
    }

    private static string? Find(string sessionId, string root)
    {
        if (!Directory.Exists(root)) return null;
        try { return Directory.EnumerateFiles(root, $"*{sessionId}.jsonl", SearchOption.AllDirectories).FirstOrDefault(); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return null; }
    }

    private static string? Valid(string? path, string sessionId, string root)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)
            || !Path.GetFileName(path).Contains(sessionId, StringComparison.Ordinal)
            || !string.Equals(Path.GetExtension(path), ".jsonl", StringComparison.OrdinalIgnoreCase)) return null;
        var fullPath = Path.GetFullPath(path);
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase) ? fullPath : null;
    }
}
