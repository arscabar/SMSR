using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class StdioDailyActivityTools(McpHttpGateway gateway)
{
    [McpServerTool(Name = "record_daily_activity"), Description("프로젝트 파일을 실제로 변경한 요청의 최종 결과를 날짜별 작업 일지에 한 번 기록합니다. 계산·검색·질문·읽기 전용 확인은 기록하지 마세요.")]
    public Task<string> RecordDailyActivity(
        string activityId, string projectId, string taskId, string title, string summary,
        string status = "SUCCESS", IReadOnlyList<string>? files = null,
        IReadOnlyList<string>? verifications = null, IReadOnlyList<string>? artifacts = null,
        string? workflowId = null, string? agentId = null)
        => gateway.CallAsync("record_daily_activity", new
        {
            activityId,
            projectId,
            taskId,
            title,
            summary,
            status,
            files,
            verifications,
            artifacts,
            workflowId,
            agentId
        });
}
