namespace SMSR.App.ViewModels;

public sealed record WorkflowChoice(string WorkflowId, string DisplayName);

public sealed partial class WorkflowSelectionViewModel
{
    private async Task LoadWorkflowsAsync(string projectId)
    {
        WorkflowIds.Clear();
        Workflows.Clear();
        foreach (var entry in await server.GetWorkflowCatalogAsync(projectId))
        {
            WorkflowIds.Add(entry.WorkflowId);
            var display = string.IsNullOrWhiteSpace(entry.Title)
                ? entry.WorkflowId
                : $"{entry.Title} · {entry.WorkflowId}";
            Workflows.Add(new(entry.WorkflowId, display));
        }
    }
}
