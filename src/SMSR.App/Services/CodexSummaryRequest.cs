namespace SMSR.App.Services;

internal static class CodexSummaryRequest
{
    public static string Instruction(string requestId)
        => $"SMSR 일자별 요약 요청 {requestId}를 처리해주세요. " +
           "SMSR MCP의 get_daily_summary_request로 자료를 읽어 요약하고 " +
           "save_daily_summary_result로 결과를 저장해주세요. 파일은 수정하지 마세요.";

    public static string Uri(string requestId)
        => $"codex://threads/new?prompt={System.Uri.EscapeDataString(Instruction(requestId))}";
}
