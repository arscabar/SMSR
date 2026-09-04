using SMSR.App.Mvp;
using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed partial class WorkflowWorkspaceViewModel
{
    private void OpenCodexSummaryRequest(DateTime date, string prompt)
    {
        var request = _host.CreateDailySummaryRequest(date, prompt);
        _pendingSummaryRequestId = request.RequestId;
        if (!_platform.TryOpenBrowser(CodexSummaryRequest.Uri(request.RequestId)))
            throw new InvalidOperationException("Codex 앱을 열 수 없습니다.");
        DailySummary = "Codex에 요약 요청을 준비했습니다. 열린 Codex 창에서 전송을 누르면 결과가 이곳에 자동 반영됩니다.";
        DailySummaryMeta = $"{date:yyyy년 M월 d일} · Gemini 미연결 · Codex 응답 대기";
    }

    private void OnDailySummaryCompleted(object? sender, DailySummaryCompletedEventArgs eventArgs)
    {
        if (eventArgs.RequestId != _pendingSummaryRequestId) return;
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null) return;
        _ = dispatcher.InvokeAsync(() =>
        {
            DailySummary = eventArgs.Content;
            DailySummaryMeta = $"{eventArgs.Date:yyyy년 M월 d일} · Codex · 생성 {DateTime.Now:HH:mm}";
            _pendingSummaryRequestId = null;
        });
    }

    private static (DateTimeOffset Start, DateTimeOffset End) LocalDayRange(DateTime date)
    {
        var startLocal = DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified);
        var endLocal = startLocal.AddDays(1);
        return (new DateTimeOffset(startLocal, TimeZoneInfo.Local.GetUtcOffset(startLocal)).ToUniversalTime(),
            new DateTimeOffset(endLocal, TimeZoneInfo.Local.GetUtcOffset(endLocal)).ToUniversalTime());
    }
}
