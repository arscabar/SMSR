using SMSR.App.Mvp;

namespace SMSR.App.ViewModels;

public sealed partial class WorkflowSelectionViewModel
{
    private async Task LoadWorkflowsAsync(string projectId)
    {
        WorkflowIds.Clear();
        Workflows.Clear();
        foreach (var entry in await server.GetWorkflowCatalogAsync(projectId))
        {
            WorkflowIds.Add(entry.WorkflowId);
            Workflows.Add(CreateChoice(projectId, entry));
        }
    }

    private static WorkflowChoice CreateChoice(string projectId, WorkflowCatalogEntry entry)
        => new(projectId, entry.WorkflowId,
            string.IsNullOrWhiteSpace(entry.Title) ? "이름 없는 이전 작업" : entry.Title,
            entry.Status, entry.NodeCount, entry.UpdatedAtUtc);
}
