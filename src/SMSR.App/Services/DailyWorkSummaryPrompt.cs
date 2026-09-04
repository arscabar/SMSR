using System.Text;
using SMSR.App.Mvp;

namespace SMSR.App.Services;

internal static class DailyWorkSummaryPrompt
{
    public static string Build(DateTime date, IReadOnlyList<WorkflowCalendarEntry> workflows,
        IReadOnlyList<DailyActivity> activities)
    {
        var text = new StringBuilder($$"""
            다음은 SMSR가 {{date:yyyy년 M월 d일}}에 수신한 작업 기록입니다.
            제공된 기록만 근거로 한국어 업무 요약을 작성하세요. 추측하거나 프롬프트·명령 원문을 재현하지 마세요.
            형식: 1) 전체 개요 2) 프로젝트별 작업·결과 3) 변경 파일과 검증 4) 실패·차단·남은 위험. 항목이 없으면 생략하세요.

            [그래프 작업]
            """);
        foreach (var item in workflows.Take(200))
            text.AppendLine($"- {item.ProjectId} | {item.Title ?? "이름 없음"} | {item.Status} | 노드 {item.NodeCount}개");
        text.AppendLine().AppendLine("[완료 작업 기록]");
        foreach (var item in activities.Take(200))
        {
            text.AppendLine($"- {item.ProjectId} | {item.Title} | {item.Status} | {item.Summary}");
            if (item.Files.Count > 0) text.AppendLine($"  파일: {string.Join(", ", item.Files.Take(20))}");
            if (item.Verifications.Count > 0) text.AppendLine($"  검증: {string.Join("; ", item.Verifications.Take(20))}");
        }
        return text.ToString();
    }
}
