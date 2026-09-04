using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class DailySummaryTools(DailySummaryCoordinator summaries)
{
    [McpServerTool(Name = "get_daily_summary_request"), Description("SMSR 앱이 Codex에 맡긴 일자별 요약 요청의 데이터와 작성 형식을 읽습니다. 사용자가 요청 ID를 제시했을 때만 호출하세요.")]
    public string GetRequest(string requestId)
    {
        var request = summaries.Get(requestId);
        return request is null ? JsonSerializer.Serialize(new { error = "요청을 찾을 수 없거나 만료되었습니다." })
            : JsonSerializer.Serialize(request);
    }

    [McpServerTool(Name = "save_daily_summary_result"), Description("get_daily_summary_request로 받은 요청의 최종 한국어 요약을 SMSR 앱에 돌려보냅니다.")]
    public string SaveResult(string requestId, string content)
    {
        var saved = summaries.Complete(requestId, content);
        return JsonSerializer.Serialize(new
        {
            requestId,
            saved,
            error = saved ? null : "요청이 없거나 요약이 비어 있습니다."
        });
    }
}
