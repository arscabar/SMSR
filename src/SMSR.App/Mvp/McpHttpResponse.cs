using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class McpHttpResponse
{
    public static string Text(string payload)
    {
        var json = payload.Split('\n')
            .FirstOrDefault(line => line.StartsWith("data: ", StringComparison.Ordinal))?[6..] ?? payload;
        using var document = JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty("result", out var result)
            && result.TryGetProperty("content", out var content) && content.GetArrayLength() > 0
            && content[0].TryGetProperty("text", out var text)
                ? text.GetString() ?? ""
                : JsonSerializer.Serialize(new { error = "SMSR MCP 응답을 읽지 못했습니다." });
    }
}
