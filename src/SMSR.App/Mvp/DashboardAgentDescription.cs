namespace SMSR.App.Mvp;

internal static class DashboardAgentDescription
{
    public static string Render(AgentState agent)
    {
        var model = DashboardPanels.Encode(agent.Model ?? "확인 불가");
        var effort = DashboardPanels.Encode(agent.ReasoningEffort ?? "확인 불가");
        var work = DashboardPanels.Encode(agent.Summary ?? "담당 작업 결과 미기록");
        var parent = agent.ParentAgentId is null ? "" :
            $"<span>상위 에이전트 {DashboardPanels.Encode(agent.ParentAgentId)}</span>";
        var explanation = agent.ReasoningEffort switch
        {
            "low" or "minimal" or "none" => "빠른 처리를 우선하는 추론 설정",
            "medium" => "속도와 추론 깊이의 균형 설정",
            "high" or "xhigh" or "max" or "ultra" => "복잡한 문제에 더 많은 추론을 사용하는 설정",
            _ => "실제 추론 설정을 확인할 수 없음"
        };
        return $"<div class=\"agent-description\"><strong>에이전트 설명</strong><p>{work}</p>"
            + $"<span>모델 {model} · 추론 강도 {effort}</span><small>{explanation}. 추론 강도는 성능 점수가 아닙니다.</small>{parent}</div>";
    }
}
