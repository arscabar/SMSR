using System.Net;
using System.Text;
using System.Linq;

namespace SMSR.App.Mvp;

public static class DashboardPage
{
    public static string Render(WorkflowState state, IReadOnlyList<RecentEvent> events)
        => Render(state, new WorkflowPlan(state.ProjectId, state.WorkflowId, []), events);

    public static string Render(WorkflowState state, WorkflowPlan plan, IReadOnlyList<RecentEvent> events)
    {
        var planNodes = new StringBuilder();
        foreach (var node in plan.Nodes)
            planNodes.Append($"<article class=\"node {Encode(node.Status)}\"><b>{Encode(node.Title)}</b><small>{Encode(node.NodeId)} · {Encode(node.Status)} · 가중치 {node.Weight}</small><small>선행: {Encode(node.DependsOn.Count == 0 ? "없음" : string.Join(", ", node.DependsOn))}</small></article>");
        if (planNodes.Length == 0) planNodes.Append("<p>저장된 계획이 없습니다.</p>");
        var totalWeight = plan.Nodes.Sum(node => node.Weight);
        var completedWeight = plan.Nodes.Where(node => node.Status == "SUCCESS").Sum(node => node.Weight);
        var progress = totalWeight == 0 ? 0 : completedWeight * 100 / totalWeight;
        var rows = new StringBuilder();
        foreach (var node in state.Nodes)
            rows.Append($"<tr><td>{Encode(node.NodeId)}</td><td>{Encode(node.Status)}</td><td>{Encode(node.AgentId)}</td><td>{Encode(node.Summary ?? "-")}</td><td>{node.UpdatedAt:O}</td></tr>");
        if (rows.Length == 0) rows.Append("<tr><td colspan=\"5\">기록된 이벤트가 없습니다.</td></tr>");

        var eventRows = new StringBuilder();
        foreach (var item in events)
            eventRows.Append($"<tr><td>{item.CreatedAt:O}</td><td>{Encode(item.NodeId)}</td><td>{Encode(item.Status)}</td><td>{Encode(item.AgentId)}</td><td>{Encode(item.Error ?? item.Summary ?? "-")}</td></tr>");
        if (eventRows.Length == 0) eventRows.Append("<tr><td colspan=\"5\">최근 이벤트가 없습니다.</td></tr>");
        var summary = string.Join(" · ", state.Nodes.GroupBy(node => node.Status).OrderBy(group => group.Key).Select(group => $"{Encode(group.Key)} {group.Count()}"));
        if (summary.Length == 0) summary = "이벤트 없음";

        return $$"""
            <!doctype html><html lang="ko"><head><meta charset="utf-8"><meta http-equiv="refresh" content="2">
            <title>SMSR 대시보드</title><style>body{font:14px system-ui;margin:32px;color:#172033}table{border-collapse:collapse;width:100%;margin-top:20px}th,td{border:1px solid #dce3ef;padding:10px;text-align:left}th{background:#edf3fb}.graph{display:grid;gap:8px;grid-template-columns:repeat(auto-fit,minmax(190px,1fr))}.node{border:1px solid #dce3ef;border-left:4px solid #78889d;padding:12px;background:#fff}.node.SUCCESS{border-left-color:#16865b}.node.FAILED,.node.BLOCKED{border-left-color:#c93d4b}.node.IN_PROGRESS{border-left-color:#2369c8}.node b,.node small{display:block;margin-bottom:5px}</style></head>
            <body><h1>SMSR 작업 상태</h1><p>프로젝트: <b>{{Encode(state.ProjectId)}}</b> · 워크플로우: <b>{{Encode(state.WorkflowId)}}</b></p>
            <p>2초마다 새로 고칩니다. 계획 진행률 {{progress}}% ({{completedWeight}}/{{totalWeight}} 가중치)</p><h2>계획 그래프</h2><section class="graph">{{planNodes}}</section>
            <p>상태 노드 {{state.Nodes.Count}}개 · 상태 요약: {{summary}}</p><table><tr><th>노드</th><th>상태</th><th>에이전트</th><th>요약</th><th>갱신</th></tr>{{rows}}</table>
            <h2>최근 이벤트</h2><table><tr><th>시각</th><th>노드</th><th>상태</th><th>에이전트</th><th>내용</th></tr>{{eventRows}}</table></body></html>
            """;
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
