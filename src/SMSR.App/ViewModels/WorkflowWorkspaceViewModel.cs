using System.Windows.Input;
using SMSR.App.Infrastructure;
using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed partial class WorkflowWorkspaceViewModel : ViewModelBase
{
    private readonly LocalServerHost _host;
    private readonly IPlatformActions _platform;
    private readonly RelayCommand _openDashboardCommand;
    private readonly RelayCommand _deleteWorkflowCommand;
    private readonly RelayCommand _deleteProjectCommand;
    private readonly RelayCommand _deleteAllCommand;
    private bool _isDeleting;
    private string _statusMessage = "워크플로우를 선택하세요.";

    public WorkflowWorkspaceViewModel(LocalServerHost host, IPlatformActions platform)
    {
        _host = host;
        _platform = platform;
        Selection = new WorkflowSelectionViewModel(host);
        Monitor = new WorkflowMonitorViewModel(host);
        Selection.PropertyChanged += OnSelectionChanged;
        Selection.WorkflowRequested += choice => _ = SelectCalendarWorkflowAsync(choice);
        RefreshSelectionCommand = new RelayCommand(() => _ = RefreshSelectionAsync());
        RefreshMonitorCommand = new RelayCommand(() => _ = RefreshMonitorAsync(), HasWorkflowSelection);
        GenerateSummaryCommand = new RelayCommand(() => _ = GenerateSummaryAsync(), HasWorkflowSelection);
        ExportCommand = new RelayCommand(() => _ = ExportAsync(), HasWorkflowSelection);
        _openDashboardCommand = new RelayCommand(OpenDashboard, HasWorkflowSelection);
        OpenDashboardCommand = _openDashboardCommand;
        _deleteWorkflowCommand = new RelayCommand(() => _ = DeleteWorkflowAsync(), HasWorkflowSelection);
        _deleteProjectCommand = new RelayCommand(() => _ = DeleteProjectAsync(), HasProjectSelection);
        _deleteAllCommand = new RelayCommand(() => _ = DeleteAllAsync(), CanDeleteAll);
        DeleteWorkflowCommand = _deleteWorkflowCommand;
        DeleteProjectCommand = _deleteProjectCommand;
        DeleteAllCommand = _deleteAllCommand;
        _host.Stopping += OnHostStopping;
        _host.StateChanged += OnHostStateChanged;
        _host.WorkflowChanged += OnWorkflowChanged;
    }

    public WorkflowSelectionViewModel Selection { get; }
    public WorkflowMonitorViewModel Monitor { get; }
    public ICommand RefreshSelectionCommand { get; }
    public ICommand RefreshMonitorCommand { get; }
    public ICommand GenerateSummaryCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand OpenDashboardCommand { get; }
    public ICommand DeleteWorkflowCommand { get; }
    public ICommand DeleteProjectCommand { get; }
    public ICommand DeleteAllCommand { get; }
    public string StatusMessage { get => _statusMessage; private set => SetField(ref _statusMessage, value); }

    public async Task LoadAsync()
    {
        await RefreshSelectionAsync();
        if (HasWorkflowSelection())
        {
            await RefreshMonitorAsync();
            StatusMessage = "저장된 작업 진행도를 복원했습니다.";
        }
    }

    private bool HasWorkflowSelection() => !_isDeleting && HasProjectSelection() && !string.IsNullOrWhiteSpace(Selection.WorkflowId);
    private bool HasProjectSelection() => !_isDeleting && _host.IsRunning && !string.IsNullOrWhiteSpace(Selection.ProjectId);
    private bool CanDeleteAll() => !_isDeleting && _host.IsRunning && Selection.ProjectIds.Count > 0;

    private void NotifyCommandStates()
    {
        foreach (var command in new[] { RefreshMonitorCommand, GenerateSummaryCommand, ExportCommand,
                     OpenDashboardCommand, DeleteWorkflowCommand, DeleteProjectCommand, DeleteAllCommand })
            ((RelayCommand)command).NotifyCanExecuteChanged();
    }
}
