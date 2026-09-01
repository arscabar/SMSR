using System.Net.Http;
using System.Net.Http.Json;
using SMSR.App.Mvp;

namespace SMSR.App.Services;

internal sealed class ActivityHookClient(string dataPath)
{
    public async Task RecordAsync(ActivityRecord record)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
            client.DefaultRequestHeaders.Add("X-SMSR-Hook-Token", new ActivityHookToken(dataPath).Value);
            using var response = await client.PostAsJsonAsync("http://127.0.0.1:49783/api/activity", record);
            if (response.IsSuccessStatusCode) return;
        }
        catch { }
        new ActivityJsonlStore(dataPath).Append(record);
    }

    public static async Task<bool> IsTerminalAsync(TrackingSession tracking)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
            var url = $"http://127.0.0.1:49783/api/plan?projectId={Uri.EscapeDataString(tracking.ProjectId)}&workflowId={Uri.EscapeDataString(tracking.WorkflowId)}";
            var plan = await client.GetFromJsonAsync<WorkflowPlan>(url);
            if (plan is null || plan.Nodes.Count == 0) return false;
            var parents = plan.Nodes.Where(node => node.ParentNodeId is not null).Select(node => node.ParentNodeId).ToHashSet();
            return plan.Nodes.Where(node => !parents.Contains(node.NodeId))
                .All(node => node.Status is "SUCCESS" or "FAILED" or "BLOCKED");
        }
        catch { return false; }
    }
}
