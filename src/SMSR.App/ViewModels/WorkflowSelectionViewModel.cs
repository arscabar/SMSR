using System.Collections.ObjectModel;
using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed class WorkflowSelectionViewModel(LocalServerHost server) : ViewModelBase
{
    private string _projectId = "";
    private string _workflowId = "";

    public ObservableCollection<string> ProjectIds { get; } = [];
    public ObservableCollection<string> WorkflowIds { get; } = [];

    public string ProjectId
    {
        get => _projectId;
        set
        {
            if (!SetField(ref _projectId, value)) return;
            WorkflowIds.Clear();
            WorkflowId = "";
        }
    }

    public string WorkflowId { get => _workflowId; set => SetField(ref _workflowId, value); }

    public async Task LoadAsync()
    {
        ProjectIds.Clear();
        foreach (var id in await server.GetProjectIdsAsync()) ProjectIds.Add(id);
        if (string.IsNullOrWhiteSpace(ProjectId) && ProjectIds.Count > 0) ProjectId = ProjectIds[0];
        WorkflowIds.Clear();
        if (string.IsNullOrWhiteSpace(ProjectId)) return;
        foreach (var id in await server.GetWorkflowIdsAsync(ProjectId)) WorkflowIds.Add(id);
        if (string.IsNullOrWhiteSpace(WorkflowId) && WorkflowIds.Count > 0) WorkflowId = WorkflowIds[0];
    }
}
