using System.IO;
using System.Text;
using System.Text.Json;

namespace SMSR.App.Services;

internal static class CodexHookRunner
{
    public static async Task RunAsync()
    {
        using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
        var input = await reader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(input)) return;
        if (await ProcessAsync(input) is { } output) await WriteAsync(output);
    }

    internal static async Task<string?> ProcessAsync(string input, Func<JsonElement, Task>? record = null)
    {
        using var document = JsonDocument.Parse(input);
        var eventName = HookJson.String(document.RootElement, "hook_event_name");
        var logFailure = record is null;
        record ??= value => CodexActivityHook.ProcessAsync(value);
        try { await record(document.RootElement); }
        catch (Exception exception) { if (logFailure) LogFailure(eventName, exception); }
        if (eventName == "UserPromptSubmit") return CodexAutoTrackingContext.CreateOutput(input);
        return eventName is "Stop" or "SubagentStop" ? "{}" : null;
    }

    private static async Task WriteAsync(string value)
    {
        await using var writer = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
        await writer.WriteAsync(value);
    }

    internal static void LogFailure(string eventName, Exception exception)
    {
        try
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMSR");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "codex-hook-last-error.txt"),
                $"{DateTimeOffset.UtcNow:O} {eventName} {exception.GetType().FullName}{Environment.NewLine}");
        }
        catch { }
    }
}
