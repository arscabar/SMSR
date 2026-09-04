using SMSR.App.Mvp;
using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed partial class WorkflowWorkspaceViewModel
{
    private async Task GenerateSelectedDateSummaryAsync()
    {
        if (Selection.SelectedDate is { } date) await GenerateDailySummaryAsync(date);
    }

    private async Task GenerateDailySummaryAsync(DateTime date)
    {
        IsSummarizing = true;
        DailySummaryMeta = $"{date:yyyy년 M월 d일} 자료를 모으는 중…";
        try
        {
            var (start, end) = LocalDayRange(date);
            var activities = await _host.GetDailyActivitiesAsync(start, end);
            var workflows = (await _host.GetWorkflowCalendarAsync())
                .Where(item => item.UpdatedAtUtc?.ToLocalTime().Date == date.Date).ToArray();
            if (activities.Count == 0 && workflows.Length == 0)
            {
                DailySummary = "선택한 날짜에 SMSR가 기록한 작업이 없습니다.";
                DailySummaryMeta = $"{date:yyyy년 M월 d일} · 기록 없음";
                return;
            }
            var prompt = DailyWorkSummaryPrompt.Build(date, workflows, activities);
            if (_geminiCredentials.Exists && await TryGeminiAsync(date, prompt)) return;
            OpenCodexSummaryRequest(date, prompt);
        }
        catch (Exception exception)
        {
            DailySummary = "요약을 생성하지 못했습니다.";
            DailySummaryMeta = exception.Message;
        }
        finally { IsSummarizing = false; }
    }

    private async Task<bool> TryGeminiAsync(DateTime date, string prompt)
    {
        try
        {
            DailySummaryMeta = $"{date:yyyy년 M월 d일} · Gemini에서 요약 중…";
            DailySummary = await _gemini.GenerateAsync(prompt);
            DailySummaryMeta = $"{date:yyyy년 M월 d일} · Gemini {GeminiSummaryClient.Model} · 생성 {DateTime.Now:HH:mm}";
            return true;
        }
        catch (Exception exception)
        {
            DailySummaryMeta = $"Gemini 연결 실패({exception.Message}) · Codex 요청으로 전환 중…";
            return false;
        }
    }

}
