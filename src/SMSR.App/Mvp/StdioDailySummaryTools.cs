using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class StdioDailySummaryTools(McpHttpGateway gateway)
{
    [McpServerTool(Name = "get_daily_summary_request"), Description("SMSR 앱이 Codex에 맡긴 일자별 요약 요청의 데이터와 작성 형식을 읽습니다. 사용자가 요청 ID를 제시했을 때만 호출하세요.")]
    public Task<string> GetRequest(string requestId)
        => gateway.CallAsync("get_daily_summary_request", new { requestId });

    [McpServerTool(Name = "save_daily_summary_result"), Description("get_daily_summary_request로 받은 요청의 최종 한국어 요약을 SMSR 앱에 돌려보냅니다.")]
    public Task<string> SaveResult(string requestId, string content)
        => gateway.CallAsync("save_daily_summary_result", new { requestId, content });
}
