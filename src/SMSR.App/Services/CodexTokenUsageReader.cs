using System.IO;
using System.Text;
using System.Text.Json;

namespace SMSR.App.Services;

internal sealed record CodexTokenUsage(long Input, long CachedInput, long Output, string RolloutPath);

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
            CodexTokenUsage? legacy = null;
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var length = (int)Math.Min(stream.Length, TailBytes);
            stream.Seek(-length, SeekOrigin.End);
            var buffer = new byte[length];
            stream.ReadExactly(buffer);
            foreach (var line in Encoding.UTF8.GetString(buffer).Split('\n').Reverse())
            {
                if (!line.Contains("token_count", StringComparison.Ordinal)
                    && !line.Contains("token_usage_record", StringComparison.Ordinal)) continue;
                try
                {
                    using var json = JsonDocument.Parse(line);
                    if (Usage(json.RootElement) is not { } usage) continue;
                    var input = usage.GetProperty("input_tokens").GetInt64();
                    var cachedInput = usage.TryGetProperty("cached_input_tokens", out var cached)
                        ? cached.GetInt64() : 0;
                    var output = usage.GetProperty("output_tokens").GetInt64();
                    if (input < 0 || cachedInput < 0 || cachedInput > input || output < 0) continue;
                    var result = new CodexTokenUsage(input, cachedInput, output, path);
                    if (json.RootElement.GetProperty("type").GetString() == "token_usage_record") return result;
                    legacy ??= result;
                }
                catch (Exception error) when (error is JsonException or InvalidOperationException
                    or KeyNotFoundException) { }
            }
            return legacy;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException) { }
        return null;
    }

    private static JsonElement? Usage(JsonElement root)
    {
        if (!root.TryGetProperty("type", out var type) || !root.TryGetProperty("payload", out var payload)) return null;
        if (type.GetString() == "token_usage_record" && payload.TryGetProperty("thread_token_usage", out var thread))
            return thread;
        return type.GetString() == "event_msg" && payload.TryGetProperty("type", out var payloadType)
            && payloadType.GetString() == "token_count" && payload.TryGetProperty("info", out var info)
            && info.TryGetProperty("total_token_usage", out var total) ? total : null;
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
