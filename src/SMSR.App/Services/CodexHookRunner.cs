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
        if (await ProcessAsync(input) is { } output) await WriteAsync(output);
    }

    internal static async Task<string?> ProcessAsync(string input, Func<JsonElement, Task>? record = null)
    {
        using var document = JsonDocument.Parse(input);
        var eventName = HookJson.String(document.RootElement, "hook_event_name");
        record ??= value => CodexActivityHook.ProcessAsync(value);
        try { await record(document.RootElement); }
        catch { }
        if (eventName == "UserPromptSubmit") return CodexAutoTrackingContext.CreateOutput(input);
        return eventName is "Stop" or "SubagentStop" ? "{}" : null;
    }

    private static async Task WriteAsync(string value)
    {
        await using var writer = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
        await writer.WriteAsync(value);
    }
}
