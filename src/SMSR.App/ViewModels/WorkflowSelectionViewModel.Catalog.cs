using SMSR.App.Mvp;

namespace SMSR.App.ViewModels;

public sealed partial class WorkflowSelectionViewModel
{
    private async Task LoadWorkflowsAsync(string projectId)
    {
        var entries = await server.GetWorkflowCatalogAsync(projectId);
        if (ProjectId != projectId) return;
        _updatingCatalog = true;
        try
        {
        WorkflowIds.Clear();
        Workflows.Clear();
        foreach (var entry in entries)
        {
            WorkflowIds.Add(entry.WorkflowId);
            Workflows.Add(CreateChoice(projectId, entry));
        }
        }
        finally { _updatingCatalog = false; }
        OnPropertyChanged(nameof(SelectedWorkflow));
    }

    private static WorkflowChoice CreateChoice(string projectId, WorkflowCatalogEntry entry)
        => new(projectId, entry.WorkflowId,
            string.IsNullOrWhiteSpace(entry.Title) ? "이름 없는 이전 작업" : entry.Title,
            entry.Status, entry.NodeCount, entry.UpdatedAtUtc);
}
