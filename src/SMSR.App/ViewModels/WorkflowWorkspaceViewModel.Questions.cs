using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed partial class WorkflowWorkspaceViewModel
{
    private async Task AskSummaryQuestionAsync()
    {
        if (Selection.SelectedDate is not { } endDate) return;
        var startDate = Selection.SummaryStartDate ?? endDate;
        var question = SummaryQuestion.Trim();
        if (question.Length is 0 or > 1000)
        {
            DailySummaryMeta = "질문은 1자 이상 1,000자 이하로 입력하세요.";
            return;
        }
        if (startDate > endDate || (endDate - startDate).TotalDays > 366)
        {
            DailySummaryMeta = "질문 기간은 시작일 이후 최대 1년까지 선택할 수 있습니다.";
            return;
        }

        var label = $"{SummaryProjectScope.Label} · {DateRangeLabel(startDate, endDate)} · 질의";
        IsSummarizing = true;
        DailySummaryMeta = $"{label} 자료를 모으는 중…";
        try
        {
            var (workflows, activities) = await LoadSummaryDataAsync(startDate, endDate);
            if (activities.Length == 0 && workflows.Length == 0)
            {
                DailySummary = "선택한 범위와 기간에 질문할 작업 기록이 없습니다.";
                DailySummaryMeta = $"{label} · 기록 없음";
                return;
            }
            var prompt = DailyWorkSummaryPrompt.BuildQuestion(startDate, endDate, SummaryProjectScope.Label,
                workflows, activities, question);
            string? geminiError = null;
            if (_geminiCredentials.Exists)
            {
                geminiError = await TryGeminiAsync(label, prompt);
                if (geminiError is null) return;
            }
            OpenCodexSummaryRequest(endDate, label, prompt, geminiError, "질의");
        }
        catch (Exception exception)
        {
            DailySummary = "질문에 답변하지 못했습니다.";
            DailySummaryMeta = exception.Message;
        }
        finally { IsSummarizing = false; }
    }
}
