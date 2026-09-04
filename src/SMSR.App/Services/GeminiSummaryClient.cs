using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace SMSR.App.Services;

internal sealed class GeminiSummaryClient(GeminiCredentialStore credentials, HttpClient? client = null)
{
    internal const string Model = "gemini-2.5-flash";
    private readonly HttpClient _client = client ?? new() { Timeout = TimeSpan.FromSeconds(60) };

    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var apiKey = credentials.Read();
        if (string.IsNullOrWhiteSpace(apiKey)) throw new InvalidOperationException("Gemini API 키가 설정되지 않았습니다.");
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent");
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = JsonContent.Create(new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { temperature = 0.2, maxOutputTokens = 2048 }
        });
        using var response = await _client.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Gemini 응답 오류({(int)response.StatusCode})");
        using var document = JsonDocument.Parse(payload);
        var parts = document.RootElement.GetProperty("candidates")[0].GetProperty("content")
            .GetProperty("parts").EnumerateArray();
        var result = string.Join("\n", parts.Select(part => part.GetProperty("text").GetString()).Where(text => text is not null));
        return string.IsNullOrWhiteSpace(result) ? throw new InvalidOperationException("Gemini 응답이 비어 있습니다.") : result.Trim();
    }

    public async Task TestAsync(CancellationToken cancellationToken = default)
    {
        var result = await GenerateAsync("SMSR 연결 확인입니다. OK만 답하세요.", cancellationToken);
        if (string.IsNullOrWhiteSpace(result)) throw new InvalidOperationException("Gemini 연결 확인 응답이 없습니다.");
    }
}
