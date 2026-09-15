using SMSR.App.Mvp;
using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed partial class WorkflowWorkspaceViewModel
{
    private async Task GenerateSelectedDateSummaryAsync()
    {
        if (Selection.SelectedDate is not { } endDate) return;
        var startDate = Selection.SummaryStartDate ?? endDate;
        if (startDate > endDate)
        {
            DailySummaryMeta = "요약 시작일은 캘린더에서 선택한 종료일보다 늦을 수 없습니다.";
            return;
        }
        if ((endDate - startDate).TotalDays > 366)
        {
            DailySummaryMeta = "요약 기간은 최대 1년까지 선택할 수 있습니다.";
            return;
        }
        await GenerateSummaryAsync(startDate, endDate);
    }

    private async Task GenerateSummaryAsync(DateTime startDate, DateTime endDate)
    {
        var label = DateRangeLabel(startDate, endDate);
        IsSummarizing = true;
        DailySummaryMeta = $"{label} 자료를 모으는 중…";
        try
        {
            var (workflows, activities) = await LoadSummaryDataAsync(startDate, endDate);
            if (activities.Length == 0 && workflows.Length == 0)
            {
                DailySummary = "선택한 기간에 SMSR가 기록한 작업이 없습니다.";
                DailySummaryMeta = $"{label} · 기록 없음";
                return;
            }
            var prompt = DailyWorkSummaryPrompt.Build(startDate, endDate, workflows, activities);
            string? geminiError = null;
            if (_geminiCredentials.Exists)
            {
                geminiError = await TryGeminiAsync(label, prompt);
                if (geminiError is null) return;
            }
            OpenCodexSummaryRequest(endDate, label, prompt, geminiError);
        }
        catch (Exception exception)
        {
            DailySummary = "요약을 생성하지 못했습니다.";
            DailySummaryMeta = exception.Message;
        }
        finally { IsSummarizing = false; }
    }

    private async Task<string?> TryGeminiAsync(string label, string prompt)
    {
        try
        {
            DailySummaryMeta = $"{label} · Gemini에서 처리 중…";
            var model = _settings.Current.GeminiModel;
            DailySummary = await _gemini.GenerateAsync(prompt, model);
            DailySummaryMeta = $"{label} · Gemini {model} · 생성 {DateTime.Now:HH:mm}";
            return null;
        }
        catch (Exception exception)
        {
            return exception.Message;
        }
    }

    private static string DateRangeLabel(DateTime startDate, DateTime endDate)
        => startDate.Date == endDate.Date ? $"{startDate:yyyy년 M월 d일}"
            : $"{startDate:yyyy년 M월 d일} ~ {endDate:yyyy년 M월 d일}";

    private async Task<(WorkflowCalendarEntry[] Workflows, DailyActivity[] Activities)> LoadSummaryDataAsync(
        DateTime startDate, DateTime endDate)
    {
        var projectId = Selection.ProjectId;
        var (start, _) = LocalDayRange(startDate);
        var (_, end) = LocalDayRange(endDate);
        var activities = (await _host.GetDailyActivitiesAsync(start, end))
            .Where(item => item.ProjectId == projectId).ToArray();
        var workflows = (await _host.GetWorkflowCalendarAsync())
            .Where(item => item.ProjectId == projectId
                && item.UpdatedAtUtc?.ToLocalTime().Date is { } date
                && date >= startDate.Date && date <= endDate.Date).ToArray();
        return (workflows, activities);
    }

}
