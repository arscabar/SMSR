using System.IO;

namespace SMSR.App.Mvp;

internal static class AgentActivitySelfCheck
{
    public static async Task RunAsync(string dataPath)
    {
        var store = new EventStore(Path.Combine(dataPath, "agents-self-check.db"));
        await store.InitializeAsync();
        var now = DateTimeOffset.UtcNow;
        ActivityRecord Child(string id, string eventName, DateTimeOffset at) =>
            new(at, "agents", "two-workers", "root", eventName, "LIFECYCLE",
                AgentId: id, AgentRole: "worker", ParentAgentId: "root",
                Model: "gpt-test",
                ReasoningEffort: eventName == "AGENT_STARTED" ? "high" : null);
        await store.RecordAgentActivityAsync(Child("child-a", "AGENT_STARTED", now));
        await store.RecordAgentActivityAsync(Child("child-b", "AGENT_STARTED", now));
        await store.RecordAsync(new("child-task", "agents", "two-workers", "node-a", "child-a",
            "NODE_STATUS_CHANGED", "SUCCESS", "검증 완료", null, null, null, "worker"));
        await store.RecordAgentActivityAsync(Child("child-a", "AGENT_STOPPED", now.AddSeconds(2)));
        await store.RecordAgentActivityAsync(Child("child-a", "AGENT_STARTED", now.AddSeconds(1))
            with { Model = "outdated-model" });
        await store.RecordAgentActivityAsync(new(now, "agents", "two-workers", "root",
            "TOOL_COMPLETED", "TOOL", AgentId: "root", Model: "gpt-root"));
        var state = await store.GetStateAsync("agents", "two-workers");
        var child = state.Agents?.SingleOrDefault(item => item.AgentId == "child-a");
        if (state.Agents?.Count != 3 || child?.Status != "STOPPED" || child.NodeId != "node-a"
            || child.Summary != "검증 완료" || child.Model != "gpt-test"
            || child.ReasoningEffort != "high" || child.ParentAgentId != "root"
            || state.Agents.Single(item => item.AgentId == "child-b").Status != "ACTIVE"
            || state.Agents.Single(item => item.AgentId == "root").Model != "gpt-root")
            throw new InvalidOperationException("하위 에이전트 상태·담당 작업 연결 검증이 실패했습니다.");
        var html = DashboardPanels.RenderAgents(state, new("agents", "two-workers", []));
        if (!html.Contains("gpt-test", StringComparison.Ordinal)
            || !html.Contains("추론 강도 high", StringComparison.Ordinal)
            || !html.Contains("검증 완료", StringComparison.Ordinal))
            throw new InvalidOperationException("에이전트 설명 카드 검증이 실패했습니다.");
    }
}
