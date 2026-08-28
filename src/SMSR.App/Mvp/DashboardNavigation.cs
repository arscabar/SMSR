using System.Net;

namespace SMSR.App.Mvp;

internal static class DashboardNavigation
{
    public static string Url(string projectId, string workflowId, string? parentNodeId = null, string? selectedNodeId = null)
    {
        var query = $"projectId={Uri.EscapeDataString(projectId)}&workflowId={Uri.EscapeDataString(workflowId)}";
        if (parentNodeId is not null) query += $"&parentNodeId={Uri.EscapeDataString(parentNodeId)}";
        if (selectedNodeId is not null) query += $"&selectedNodeId={Uri.EscapeDataString(selectedNodeId)}";
        return "/dashboard?" + query;
    }

    public static string Breadcrumb(string projectId, string workflowId, WorkflowPlan plan, string? parentNodeId)
    {
        var nodes = plan.Nodes.ToDictionary(node => node.NodeId);
        var chain = new Stack<PlanNodeState>();
        var current = parentNodeId;
        while (current is not null && nodes.TryGetValue(current, out var node))
        {
            chain.Push(node);
            current = node.ParentNodeId;
        }
        var items = new List<string> { $"<a href=\"{Encode(Url(projectId, workflowId))}\">전체</a>" };
        while (chain.Count > 0)
        {
            var node = chain.Pop();
            items.Add($"<a href=\"{Encode(Url(projectId, workflowId, node.NodeId))}\">{Encode(node.Title)}</a>");
        }
        return $"<nav class=\"breadcrumb\">{string.Join("<span>›</span>", items)}</nav>";
    }

    public static string Encode(string value) => WebUtility.HtmlEncode(value);
}
