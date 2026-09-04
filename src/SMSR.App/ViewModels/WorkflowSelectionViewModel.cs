using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using SMSR.App.Infrastructure;
using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed partial class WorkflowSelectionViewModel : ViewModelBase
{
    private readonly LocalServerHost server;
    private readonly string _selectionPath;
    private string _projectId = "";
    private string _workflowId = "";

    public WorkflowSelectionViewModel(LocalServerHost server)
    {
        this.server = server;
        _selectionPath = Path.Combine(server.DataPath, "last-workflow.json");
        PreviousMonthCommand = new RelayCommand(() => _ = MoveMonthAsync(-1));
        NextMonthCommand = new RelayCommand(() => _ = MoveMonthAsync(1));
        TodayCommand = new RelayCommand(() => _ = ShowTodayAsync());
    }

    public ObservableCollection<string> ProjectIds { get; } = [];
    public ObservableCollection<string> WorkflowIds { get; } = [];
    public ObservableCollection<WorkflowChoice> Workflows { get; } = [];
    public ObservableCollection<DailyActivityItem> DailyActivities { get; } = [];
    public ICommand PreviousMonthCommand { get; }
    public ICommand NextMonthCommand { get; }
    public ICommand TodayCommand { get; }

    public string ProjectId
    {
        get => _projectId;
        set
        {
            if (!SetField(ref _projectId, value)) return;
            WorkflowIds.Clear();
            Workflows.Clear();
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
        await LoadCalendarAsync();
        WorkflowIds.Clear();
        if (string.IsNullOrWhiteSpace(ProjectId)) return;
        await LoadWorkflowsAsync(ProjectId);
        if (string.IsNullOrWhiteSpace(WorkflowId) && WorkflowIds.Count > 0) WorkflowId = saved is not null && WorkflowIds.Contains(saved.WorkflowId) ? saved.WorkflowId : WorkflowIds[0];
    }

    public async Task SelectAsync(string projectId, string workflowId)
    {
        var projects = await server.GetProjectIdsAsync();
        ProjectIds.Clear();
        foreach (var id in projects) ProjectIds.Add(id);
        if (!ProjectIds.Contains(projectId)) return;
        ProjectId = projectId;
        await LoadWorkflowsAsync(projectId);
        if (WorkflowIds.Contains(workflowId)) WorkflowId = workflowId;
    }

    public async Task ReloadCalendarAsync()
        => await LoadCalendarAsync();

    public void Reset()
    {
        ProjectId = "";
        WorkflowId = "";
        ProjectIds.Clear();
        WorkflowIds.Clear();
        Workflows.Clear();
        CalendarWorkflows.Clear();
        DailyActivities.Clear();
        CalendarDays.Clear();
        _calendarSource.Clear();
        _dailyCalendarSource.Clear();
        SelectedDate = null;
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
