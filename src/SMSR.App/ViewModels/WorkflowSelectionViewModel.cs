using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed class WorkflowSelectionViewModel(LocalServerHost server) : ViewModelBase
{
    private readonly string _selectionPath = Path.Combine(server.DataPath, "last-workflow.json");
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

    public string WorkflowId
    {
        get => _workflowId;
        set
        {
            if (SetField(ref _workflowId, value) && !string.IsNullOrWhiteSpace(ProjectId) && !string.IsNullOrWhiteSpace(value)) Save();
        }
    }

    public async Task LoadAsync()
    {
        var saved = LoadSaved();
        ProjectIds.Clear();
        foreach (var id in await server.GetProjectIdsAsync()) ProjectIds.Add(id);
        if (string.IsNullOrWhiteSpace(ProjectId) && ProjectIds.Count > 0) ProjectId = saved is not null && ProjectIds.Contains(saved.ProjectId) ? saved.ProjectId : ProjectIds[0];
        WorkflowIds.Clear();
        if (string.IsNullOrWhiteSpace(ProjectId)) return;
        foreach (var id in await server.GetWorkflowIdsAsync(ProjectId)) WorkflowIds.Add(id);
        if (string.IsNullOrWhiteSpace(WorkflowId) && WorkflowIds.Count > 0) WorkflowId = saved is not null && WorkflowIds.Contains(saved.WorkflowId) ? saved.WorkflowId : WorkflowIds[0];
    }

    private LastWorkflow? LoadSaved()
    {
        try { return File.Exists(_selectionPath) ? JsonSerializer.Deserialize<LastWorkflow>(File.ReadAllText(_selectionPath)) : null; }
        catch { return null; }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_selectionPath)!);
            File.WriteAllText(_selectionPath, JsonSerializer.Serialize(new LastWorkflow(ProjectId, WorkflowId)));
        }
        catch { }
    }

    private sealed record LastWorkflow(string ProjectId, string WorkflowId);
}
