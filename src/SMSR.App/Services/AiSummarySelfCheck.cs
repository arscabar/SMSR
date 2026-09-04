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
        await new GeminiSummaryClient(credentials, new HttpClient(handler)).TestAsync();
        if (handler.ApiKey != "test-key" || !handler.Body.Contains("SMSR", StringComparison.Ordinal)
            || handler.RequestUri?.AbsoluteUri.Contains(GeminiSummaryClient.Model, StringComparison.Ordinal) != true)
            throw new InvalidOperationException("Gemini 요청 계약 검증이 실패했습니다.");
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
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            const string json = "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"OK\"}]}}]}";
            return new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }
}
