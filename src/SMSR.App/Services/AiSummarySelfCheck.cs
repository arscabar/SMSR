using System.Net;
using System.Net.Http;
using System.Text;
using SMSR.App.Mvp;

namespace SMSR.App.Services;

internal static class AiSummarySelfCheck
{
    public static async Task RunAsync(string dataPath)
    {
        var credentials = new GeminiCredentialStore(dataPath);
        credentials.Save("test-key");
        if (credentials.Read() != "test-key") throw new InvalidOperationException("Gemini DPAPI 저장 검증이 실패했습니다.");
        var handler = new GeminiHandler();
        var client = new GeminiSummaryClient(credentials, new HttpClient(handler));
        var models = await client.GetModelsAsync();
        await client.TestAsync("gemini-3-test");
        var modernBody = handler.Body;
        var modernUri = handler.RequestUri;
        await client.TestAsync("gemini-2.5-test");
        if (handler.ApiKey != "test-key" || !modernBody.Contains("\"thinkingLevel\":\"low\"", StringComparison.Ordinal)
            || modernBody.Contains("temperature", StringComparison.Ordinal)
            || modernUri?.AbsoluteUri.Contains("gemini-3-test", StringComparison.Ordinal) != true
            || !handler.Body.Contains("SMSR", StringComparison.Ordinal)
            || !handler.Body.Contains("temperature", StringComparison.Ordinal)
            || handler.Body.Contains("thinkingLevel", StringComparison.Ordinal)
            || handler.RequestUri?.AbsoluteUri.Contains("gemini-2.5-test", StringComparison.Ordinal) != true
            || models.Single() != "gemini-test")
            throw new InvalidOperationException("Gemini 요청 계약 검증이 실패했습니다.");
        var settings = new AppSettingsService(dataPath);
        settings.Save(settings.Current with { GeminiModel = "models/gemini-test" });
        if (new AppSettingsService(dataPath).Current.GeminiModel != "gemini-test")
            throw new InvalidOperationException("Gemini 모델 설정 저장 검증이 실패했습니다.");
        var periodPrompt = DailyWorkSummaryPrompt.Build(new(2026, 9, 1), new(2026, 9, 15), [], []);
        if (!periodPrompt.Contains("2026년 9월 1일부터 2026년 9월 15일까지", StringComparison.Ordinal))
            throw new InvalidOperationException("Gemini 기간 요약 프롬프트 검증이 실패했습니다.");
        var questionPrompt = DailyWorkSummaryPrompt.BuildQuestion(new(2026, 9, 15), new(2026, 9, 15),
            "SMSR", [], [], "왜 필요한가요?");
        if (!questionPrompt.Contains("SMSR 프로젝트", StringComparison.Ordinal)
            || !questionPrompt.Contains("왜 필요한가요?", StringComparison.Ordinal)
            || !questionPrompt.Contains("이유나 필요성이 기록되지 않았다면", StringComparison.Ordinal))
            throw new InvalidOperationException("프로젝트 기간 질의 프롬프트 검증이 실패했습니다.");
        if (!GeminiSummaryClient.CanOfferFallback("Gemini 응답 오류(503)")
            || GeminiSummaryClient.CanOfferFallback("Gemini 응답 오류(403)")
            || GeminiSummaryClient.SelectFallbackModel("gemini-3.8-flash",
                ["gemini-3.8-flash", "gemini-2.5-flash", "gemini-2.0-flash"]) != "gemini-2.5-flash")
            throw new InvalidOperationException("Gemini 대체 모델 선택 검증이 실패했습니다.");
        credentials.Delete();
        if (credentials.Exists) throw new InvalidOperationException("Gemini 키 삭제 검증이 실패했습니다.");

        var coordinator = new DailySummaryCoordinator();
        var request = coordinator.Create(DateTime.Today, "요약 자료");
        DailySummaryCompletedEventArgs? completed = null;
        coordinator.Completed += (_, item) => completed = item;
        if (coordinator.Get(request.RequestId)?.Prompt != "요약 자료"
            || !coordinator.Complete(request.RequestId, "완료 요약")
            || completed?.Content != "완료 요약" || coordinator.Get(request.RequestId) is not null)
            throw new InvalidOperationException("Codex 요약 요청 회수 검증이 실패했습니다.");
        var uri = CodexSummaryRequest.Uri(request.RequestId);
        if (!uri.StartsWith("codex://threads/new?prompt=", StringComparison.Ordinal)
            || !Uri.UnescapeDataString(uri).Contains("save_daily_summary_result", StringComparison.Ordinal))
            throw new InvalidOperationException("Codex 요약 딥링크 검증이 실패했습니다.");
    }

    private sealed class GeminiHandler : HttpMessageHandler
    {
        public string ApiKey { get; private set; } = "";
        public string Body { get; private set; } = "";
        public Uri? RequestUri { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ApiKey = request.Headers.GetValues("x-goog-api-key").Single();
            RequestUri = request.RequestUri;
            Body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            var json = request.Method == HttpMethod.Get
                ? "{\"models\":[{\"name\":\"models/gemini-test\",\"supportedGenerationMethods\":[\"generateContent\"]}]}"
                : "{\"candidates\":[{\"content\":{\"parts\":[{\"thoughtSignature\":\"test\"},{\"text\":\"OK\"}]}}]}";
            return new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }
}
