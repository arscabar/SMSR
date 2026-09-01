using System.Net;
using System.Text;

namespace SMSR.App.Mvp;

internal static class DashboardGraph
{
    public static string Render(WorkflowPlan plan, WorkflowState state, string? parentNodeId = null)
    {
        var layer = plan.Nodes.Where(node => node.ParentNodeId == parentNodeId).ToArray();
        if (layer.Length == 0) return "<p>이 계층에 저장된 계획이 없습니다.</p>";
        var map = plan.Nodes.ToDictionary(node => node.NodeId);
        var layerIds = layer.Select(node => node.NodeId).ToHashSet();
        var context = parentNodeId is not null && map.TryGetValue(parentNodeId, out var parent) ? parent : null;
        var projectedChildren = layer.Select(node => node with
        {
            DependsOn = ProjectDependencies(node, parentNodeId, map, layerIds)
                .Concat(context is null ? [] : [context.NodeId]).Distinct().ToArray()
        });
        var projected = (context is null ? projectedChildren : [context with { DependsOn = [] }, .. projectedChildren]).ToArray();
        var currentNodeId = DashboardCurrentNode.Visible(plan, state, projected.Select(node => node.NodeId).ToHashSet());
        var layerPlan = new WorkflowPlan(plan.ProjectId, plan.WorkflowId, projected);
        var layout = DashboardGraphLayout.Create(layerPlan);
        var positions = layout.Positions;
        var agents = state.Nodes.ToDictionary(node => node.NodeId, node => node.AgentId);
        var childCounts = plan.Nodes.Where(node => node.ParentNodeId is not null).GroupBy(node => node.ParentNodeId!).ToDictionary(group => group.Key, group => group.Count());
        var svg = new StringBuilder($"<svg class=\"flow-svg\" role=\"img\" aria-label=\"작업 의존성 순서도\" width=\"{layout.Width}\" height=\"{layout.Height}\" viewBox=\"0 0 {layout.Width} {layout.Height}\"><defs><marker id=\"arrow\" markerWidth=\"8\" markerHeight=\"8\" refX=\"7\" refY=\"4\" orient=\"auto\"><path d=\"M0,0 L8,4 L0,8 Z\" /></marker></defs>");
        foreach (var node in projected)
            foreach (var dependency in node.DependsOn.Where(positions.ContainsKey))
            {
                var from = positions[dependency]; var to = positions[node.NodeId];
                var x1 = from.X + DashboardGraphLayout.NodeWidth / 2; var y1 = from.Y + DashboardGraphLayout.NodeHeight;
                var x2 = to.X + DashboardGraphLayout.NodeWidth / 2; var y2 = to.Y;
                var middle = (y1 + y2) / 2;
                svg.Append($"<path class=\"edge {Status(DashboardHierarchy.DisplayStatus(node, plan.Nodes))}\" d=\"M{x1},{y1} C{x1},{middle} {x2},{middle} {x2},{y2}\" marker-end=\"url(#arrow)\" />");
            }
        foreach (var node in projected)
        {
            var position = positions[node.NodeId];
            var center = position.X + DashboardGraphLayout.NodeWidth / 2;
            var agent = agents.GetValueOrDefault(node.NodeId, node.AssignedAgentId ?? "-");
            var children = childCounts.GetValueOrDefault(node.NodeId);
            var displayStatus = DashboardHierarchy.DisplayStatus(node, plan.Nodes);
            var isContext = parentNodeId is not null && parentNodeId == node.NodeId;
            var originalParent = map[node.NodeId].ParentNodeId;
            var target = isContext
                ? DashboardNavigation.Url(plan.ProjectId, plan.WorkflowId, originalParent)
                : children > 0 ? DashboardNavigation.Url(plan.ProjectId, plan.WorkflowId, node.NodeId) : DashboardNavigation.Url(plan.ProjectId, plan.WorkflowId, parentNodeId, node.NodeId);
            var stateNode = state.Nodes.FirstOrDefault(item => item.NodeId == node.NodeId);
            var effectiveProgress = displayStatus == "PENDING" && (stateNode?.Status ?? node.Status) != "PENDING"
                ? 0 : WorkflowProgress.Value(stateNode?.Status ?? node.Status, stateNode?.ProgressPercentage);
            var drill = isContext ? "현재 상위 작업" : children > 0 ? $"↳ 하위 작업 {children}개" : $"진행 {effectiveProgress}%";
            var meta = $"{Trim(node.NodeId, 12)} · {ShortId(agent)} · {displayStatus}";
            var current = node.NodeId == currentNodeId ? " current" : string.Empty;
            svg.Append($"<a href=\"{Encode(target)}\"><g class=\"flow-node {Status(displayStatus)}{current}\"><title>{Encode(node.Title)} · {Encode(node.NodeId)} · {Encode(agent)} · {Encode(displayStatus)}</title><rect x=\"{position.X}\" y=\"{position.Y}\" width=\"{DashboardGraphLayout.NodeWidth}\" height=\"{DashboardGraphLayout.NodeHeight}\" rx=\"12\"/><text x=\"{center}\" y=\"{position.Y + 23}\"><tspan class=\"node-title\">{Encode(Trim(node.Title, 15))}</tspan><tspan class=\"node-meta\" x=\"{center}\" dy=\"20\">{Encode(meta)}</tspan><tspan class=\"node-drill\" x=\"{center}\" dy=\"18\">{Encode(drill)}</tspan></text></g></a>");
        }
        return svg.Append("</svg>").ToString();
    }

    private static string Status(string value) => value is "SUCCESS" or "IN_PROGRESS" or "VALIDATING" or "FAILED" or "RETRYING" or "BLOCKED" ? value : "PENDING";
    private static IReadOnlyList<string> ProjectDependencies(PlanNodeState node, string? parentNodeId,
        IReadOnlyDictionary<string, PlanNodeState> map, IReadOnlySet<string> layerIds)
        => node.DependsOn.Select(id => Project(id, parentNodeId, map)).Where(id => id is not null && id != node.NodeId && layerIds.Contains(id)).Select(id => id!).Distinct().ToArray();

    private static string? Project(string id, string? parentNodeId, IReadOnlyDictionary<string, PlanNodeState> map)
    {
        if (!map.TryGetValue(id, out var current)) return null;
        while (current.ParentNodeId != parentNodeId)
        {
            if (current.ParentNodeId is null || !map.TryGetValue(current.ParentNodeId, out current)) return null;
        }
        return current.NodeId;
    }

    private static string ShortId(string value) => value.Length <= 18 ? value : $"{value[..8]}…{value[^4..]}";
    private static string Trim(string value, int length) => value.Length <= length ? value : value[..(length - 1)] + "…";
    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
