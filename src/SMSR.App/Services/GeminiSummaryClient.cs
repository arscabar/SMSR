using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace SMSR.App.Services;

internal sealed class GeminiSummaryClient(GeminiCredentialStore credentials, HttpClient? client = null)
{
    internal const string DefaultModel = "gemini-3.8-flash";
    private readonly HttpClient _client = client ?? new() { Timeout = TimeSpan.FromSeconds(60) };

    public async Task<IReadOnlyList<string>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get,
            "https://generativelanguage.googleapis.com/v1beta/models?pageSize=1000");
        using var response = await _client.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, payload);
        using var document = JsonDocument.Parse(payload);
        return document.RootElement.GetProperty("models").EnumerateArray()
            .Where(model => model.GetProperty("supportedGenerationMethods").EnumerateArray()
                .Any(method => method.GetString() == "generateContent"))
            .Select(model => model.GetProperty("name").GetString()?.Replace("models/", ""))
            .Where(name => !string.IsNullOrWhiteSpace(name) && name.StartsWith("gemini-", StringComparison.Ordinal)
                && !name.Contains("-image", StringComparison.Ordinal)
                && !name.Contains("-tts", StringComparison.Ordinal)
                && !name.Contains("transcribe", StringComparison.Ordinal))
            .Cast<string>()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<string> GenerateAsync(string prompt, string model,
        CancellationToken cancellationToken = default)
    {
        model = NormalizeModel(model);
        using var request = CreateRequest(HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent");
        var generationConfig = new Dictionary<string, object> { ["maxOutputTokens"] = 2048 };
        if (UsesThinkingLevel(model))
            generationConfig["thinkingConfig"] = new { thinkingLevel = "low" };
        else
            generationConfig["temperature"] = 0.2;
        request.Content = JsonContent.Create(new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig
        });
        using var response = await _client.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, payload);
        using var document = JsonDocument.Parse(payload);
        if (!document.RootElement.TryGetProperty("candidates", out var candidates)
            || candidates.GetArrayLength() == 0)
            throw new InvalidOperationException("Gemini 응답 후보가 없습니다. 선택 모델의 출력 설정을 확인하세요.");
        var parts = candidates[0].GetProperty("content")
            .GetProperty("parts").EnumerateArray();
        var result = string.Join("\n", parts.Select(part => part.TryGetProperty("text", out var text)
            ? text.GetString() : null).Where(text => text is not null));
        return string.IsNullOrWhiteSpace(result) ? throw new InvalidOperationException("Gemini 응답이 비어 있습니다.") : result.Trim();
    }

    public async Task TestAsync(string model, CancellationToken cancellationToken = default)
    {
        var result = await GenerateAsync("SMSR 연결 확인입니다. OK만 답하세요.", model, cancellationToken);
        if (string.IsNullOrWhiteSpace(result)) throw new InvalidOperationException("Gemini 연결 확인 응답이 없습니다.");
    }

    internal static string NormalizeModel(string? model)
        => string.IsNullOrWhiteSpace(model) ? DefaultModel : model.Trim().Replace("models/", "");

    internal static bool UsesThinkingLevel(string model)
        => model.StartsWith("gemini-3", StringComparison.Ordinal)
            || model.EndsWith("-latest", StringComparison.Ordinal);

    internal static bool CanOfferFallback(string error)
        => new[] { "(404)", "(429)", "(503)" }.Any(error.Contains);

    internal static string? SelectFallbackModel(string current, IReadOnlyList<string> models)
        => models.FirstOrDefault(model => model.Equals("gemini-2.5-flash", StringComparison.OrdinalIgnoreCase)
                && !model.Equals(current, StringComparison.OrdinalIgnoreCase))
            ?? models.FirstOrDefault(model => model.Contains("flash", StringComparison.OrdinalIgnoreCase)
                && !model.Contains("preview", StringComparison.OrdinalIgnoreCase)
                && !model.Equals(current, StringComparison.OrdinalIgnoreCase));

    private HttpRequestMessage CreateRequest(HttpMethod method, string uri)
    {
        var apiKey = credentials.Read();
        if (string.IsNullOrWhiteSpace(apiKey)) throw new InvalidOperationException("Gemini API 키가 설정되지 않았습니다.");
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("x-goog-api-key", apiKey);
        return request;
    }

    private static void EnsureSuccess(HttpResponseMessage response, string payload)
    {
        if (response.IsSuccessStatusCode) return;
        try
        {
            using var document = JsonDocument.Parse(payload);
            var message = document.RootElement.GetProperty("error").GetProperty("message").GetString();
            throw new InvalidOperationException($"Gemini 응답 오류({(int)response.StatusCode}): {message}");
        }
        catch (JsonException)
        {
            throw new InvalidOperationException($"Gemini 응답 오류({(int)response.StatusCode})");
        }
    }
}
