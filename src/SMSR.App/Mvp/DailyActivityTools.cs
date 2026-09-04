using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class DailyActivityTools(EventStore events, DailyActivityNotifier notifier)
{
    [McpServerTool(Name = "record_daily_activity"), Description("프로젝트 파일을 실제로 변경한 요청의 최종 결과를 날짜별 작업 일지에 한 번 기록합니다. 계산·검색·질문·읽기 전용 확인은 기록하지 마세요.")]
    public async Task<string> RecordDailyActivity(
        string activityId, string projectId, string taskId, string title, string summary,
        string status = "SUCCESS", IReadOnlyList<string>? files = null,
        IReadOnlyList<string>? verifications = null, IReadOnlyList<string>? artifacts = null,
        string? workflowId = null, string? agentId = null)
    {
        var request = new DailyActivityRequest(activityId, projectId, taskId, title, summary,
            status, files, verifications, artifacts, workflowId, agentId);
        if (DailyActivityValidation.Validate(request) is { } error)
            return JsonSerializer.Serialize(new { error });
        await events.RecordDailyActivityAsync(request);
        notifier.Publish(projectId);
        return JsonSerializer.Serialize(new { activityId, recorded = true });
    }
}
