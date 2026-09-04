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
    private readonly RelayCommand _generateTodaySummaryCommand;
    private readonly RelayCommand _generateSelectedDateSummaryCommand;
    private readonly GeminiCredentialStore _geminiCredentials;
    private readonly GeminiSummaryClient _gemini;
    private bool _isDeleting;
    private bool _isSummarizing;
    private string _statusMessage = "워크플로우를 선택하세요.";
    private string _dailySummary = "요약할 날짜를 선택하거나 금일 작업 요약을 실행하세요.";
    private string _dailySummaryMeta = "모든 프로젝트의 그래프와 일일 작업 기록을 함께 요약합니다.";
    private string? _pendingSummaryRequestId;

    public WorkflowWorkspaceViewModel(LocalServerHost host, IPlatformActions platform)
    {
        _host = host;
        _platform = platform;
        _geminiCredentials = new GeminiCredentialStore(host.DataPath);
        _gemini = new GeminiSummaryClient(_geminiCredentials);
        Selection = new WorkflowSelectionViewModel(host);
        Monitor = new WorkflowMonitorViewModel(host);
        Selection.PropertyChanged += OnSelectionChanged;
        Selection.WorkflowRequested += choice => _ = SelectCalendarWorkflowAsync(choice);
        Selection.DailyActivityRequested += item => _ = SelectDailyActivityAsync(item);
        RefreshSelectionCommand = new RelayCommand(() => _ = RefreshSelectionAsync());
        RefreshMonitorCommand = new RelayCommand(() => _ = RefreshMonitorAsync(), HasWorkflowSelection);
        GenerateSummaryCommand = new RelayCommand(() => _ = GenerateSummaryAsync(), HasWorkflowSelection);
        _generateTodaySummaryCommand = new RelayCommand(() => _ = GenerateDailySummaryAsync(DateTime.Today), CanSummarize);
        _generateSelectedDateSummaryCommand = new RelayCommand(() => _ = GenerateSelectedDateSummaryAsync(), CanSummarizeSelectedDate);
        GenerateTodaySummaryCommand = _generateTodaySummaryCommand;
        GenerateSelectedDateSummaryCommand = _generateSelectedDateSummaryCommand;
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
        _host.DailyActivityChanged += OnDailyActivityChanged;
        _host.DailySummaryCompleted += OnDailySummaryCompleted;
    }

    public WorkflowSelectionViewModel Selection { get; }
    public WorkflowMonitorViewModel Monitor { get; }
    public ICommand RefreshSelectionCommand { get; }
    public ICommand RefreshMonitorCommand { get; }
    public ICommand GenerateSummaryCommand { get; }
    public ICommand GenerateTodaySummaryCommand { get; }
    public ICommand GenerateSelectedDateSummaryCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand OpenDashboardCommand { get; }
    public ICommand DeleteWorkflowCommand { get; }
    public ICommand DeleteProjectCommand { get; }
    public ICommand DeleteAllCommand { get; }
    public string StatusMessage { get => _statusMessage; private set => SetField(ref _statusMessage, value); }
    public string DailySummary { get => _dailySummary; private set => SetField(ref _dailySummary, value); }
    public string DailySummaryMeta { get => _dailySummaryMeta; private set => SetField(ref _dailySummaryMeta, value); }
    public bool IsSummarizing { get => _isSummarizing; private set { if (SetField(ref _isSummarizing, value)) NotifyCommandStates(); } }

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
    private bool CanSummarize() => !_isSummarizing && _host.IsRunning;
    private bool CanSummarizeSelectedDate() => CanSummarize() && Selection.SelectedDate is not null;

    private void NotifyCommandStates()
    {
        foreach (var command in new[] { RefreshMonitorCommand, GenerateSummaryCommand, ExportCommand,
                     OpenDashboardCommand, DeleteWorkflowCommand, DeleteProjectCommand, DeleteAllCommand })
            ((RelayCommand)command).NotifyCanExecuteChanged();
        _generateTodaySummaryCommand.NotifyCanExecuteChanged();
        _generateSelectedDateSummaryCommand.NotifyCanExecuteChanged();
    }
}
