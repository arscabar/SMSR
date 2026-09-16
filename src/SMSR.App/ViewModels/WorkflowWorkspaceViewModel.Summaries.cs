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
        var label = $"{SummaryProjectScope.Label} · {DateRangeLabel(startDate, endDate)}";
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
        var model = _settings.Current.GeminiModel;
        try
        {
            DailySummaryMeta = $"{label} · Gemini에서 처리 중…";
            DailySummary = await _gemini.GenerateAsync(prompt, model);
            DailySummaryMeta = $"{label} · Gemini {model} · 생성 {DateTime.Now:HH:mm}";
            return null;
        }
        catch (Exception exception)
        {
            if (!GeminiSummaryClient.CanOfferFallback(exception.Message)) return exception.Message;
            try
            {
                var fallback = GeminiSummaryClient.SelectFallbackModel(model, await _gemini.GetModelsAsync());
                if (fallback is null || !_platform.Confirm("Gemini 대체 모델 사용",
                        $"현재 모델 '{model}'을 사용할 수 없습니다.\n\n이번 요청만 '{fallback}'로 다시 시도할까요?\n기본 모델 설정은 변경되지 않습니다."))
                    return exception.Message;
                DailySummaryMeta = $"{label} · Gemini {fallback}로 다시 처리 중…";
                DailySummary = await _gemini.GenerateAsync(prompt, fallback);
                DailySummaryMeta = $"{label} · Gemini {fallback} · 생성 {DateTime.Now:HH:mm}";
                return null;
            }
            catch (Exception fallbackException)
            {
                return $"{exception.Message} / 대체 모델 실패: {fallbackException.Message}";
            }
        }
    }

    private static string DateRangeLabel(DateTime startDate, DateTime endDate)
        => startDate.Date == endDate.Date ? $"{startDate:yyyy년 M월 d일}"
            : $"{startDate:yyyy년 M월 d일} ~ {endDate:yyyy년 M월 d일}";

    private async Task<(WorkflowCalendarEntry[] Workflows, DailyActivity[] Activities)> LoadSummaryDataAsync(
        DateTime startDate, DateTime endDate)
    {
        var (start, _) = LocalDayRange(startDate);
        var (_, end) = LocalDayRange(endDate);
        var activities = (await _host.GetDailyActivitiesAsync(start, end))
            .Where(item => IncludesSummaryProject(item.ProjectId)).ToArray();
        var workflows = (await _host.GetWorkflowCalendarAsync())
            .Where(item => IncludesSummaryProject(item.ProjectId)
                && item.UpdatedAtUtc?.ToLocalTime().Date is { } date
                && date >= startDate.Date && date <= endDate.Date).ToArray();
        return (workflows, activities);
    }

}
