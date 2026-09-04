using SMSR.App.Mvp;

namespace SMSR.App.Services;

public sealed class LocalServerHost(string? dataPath = null, int port = LocalServer.Port,
    Func<string>? dashboardTheme = null) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _dataPath = ResolveDataPath(dataPath);
    private readonly LocalActivityLog _log = new(System.IO.Path.Combine(ResolveDataPath(dataPath), "logs"));
    private LocalServer? _server;
    private bool _isCodexAuthorized;
    private bool _isCodexConnected;
    private DateTimeOffset? _lastMcpActivityAt;

    public event EventHandler? StateChanged;
    public event EventHandler? Stopping;
    public event EventHandler? AuthorizationChanged;
    public event EventHandler<WorkflowChangedEventArgs>? WorkflowChanged;
    public event EventHandler<DailyActivityChangedEventArgs>? DailyActivityChanged;
    public bool IsRunning => _server is not null;
    public string Address => _server?.Address ?? "";
    public string LogPath => _log.Path;
    public string DataPath => _dataPath;
    public bool IsCodexAuthorized => _isCodexAuthorized;
    public bool IsCodexConnected => _isCodexConnected;
    public DateTimeOffset? LastMcpActivityAt => _lastMcpActivityAt;

    public async Task StartAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_server is null)
            {
                _server = await LocalServer.StartAsync(_dataPath, port, dashboardTheme);
                _server.AuthorizationChanged += OnAuthorizationChanged;
                _server.ConnectionChanged += OnConnectionChanged;
                _server.WorkflowChanged += OnWorkflowChanged;
                _server.DailyActivityChanged += OnDailyActivityChanged;
                _isCodexAuthorized = _server.HasAuthorizedCodex;
                UpdateConnectionState();
                await WriteLogAsync("server started");
            }
        }
        catch
        {
            await WriteLogAsync("server start failed");
            throw;
        }
        finally { _gate.Release(); }
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task StopAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_server is not null)
            {
                Stopping?.Invoke(this, EventArgs.Empty);
                _server.AuthorizationChanged -= OnAuthorizationChanged;
                _server.ConnectionChanged -= OnConnectionChanged;
                _server.WorkflowChanged -= OnWorkflowChanged;
                _server.DailyActivityChanged -= OnDailyActivityChanged;
                await _server.DisposeAsync();
                await WriteLogAsync("server stopped");
            }
            _server = null;
            UpdateConnectionState();
        }
        finally { _gate.Release(); }
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public Task<IReadOnlyList<string>> GetProjectIdsAsync() => Server.GetProjectIdsAsync();
    public Task<IReadOnlyList<string>> GetWorkflowIdsAsync(string projectId) => Server.GetWorkflowIdsAsync(projectId);
    public Task<IReadOnlyList<WorkflowCatalogEntry>> GetWorkflowCatalogAsync(string projectId) => Server.GetWorkflowCatalogAsync(projectId);
    public Task<IReadOnlyList<WorkflowCalendarEntry>> GetWorkflowCalendarAsync() => Server.GetWorkflowCalendarAsync();
    public Task<IReadOnlyList<DailyActivity>> GetDailyActivitiesAsync(DateTimeOffset startUtc, DateTimeOffset endUtc)
        => Server.GetDailyActivitiesAsync(startUtc, endUtc);
    public Task<DateTimeOffset?> GetLatestDailyActivityAtAsync() => Server.GetLatestDailyActivityAtAsync();
    public Task<WorkflowState> GetStateAsync(string projectId, string workflowId) => Server.GetStateAsync(projectId, workflowId);
    public Task<IReadOnlyList<RecentEvent>> GetRecentEventsAsync(string projectId, string workflowId) => Server.GetRecentEventsAsync(projectId, workflowId);
    public Task<WorkflowSummary?> GetLatestSummaryAsync(string projectId, string workflowId) => Server.GetLatestSummaryAsync(projectId, workflowId);
    public Task<WorkflowSummary> GenerateSummaryAsync(string projectId, string workflowId) => Server.GenerateSummaryAsync(projectId, workflowId);
    public Task<ExportResult> ExportAsync(string projectId, string workflowId) => Server.ExportAsync(projectId, workflowId);
    public async Task<int> DeleteWorkflowAsync(string projectId, string workflowId)
    {
        var count = await Server.DeleteWorkflowAsync(projectId, workflowId);
        new TrackingSessionStore(_dataPath).RemoveWorkflow(projectId, workflowId);
        return count;
    }
    public async Task<int> DeleteProjectAsync(string projectId)
    {
        var count = await Server.DeleteProjectAsync(projectId);
        new TrackingSessionStore(_dataPath).RemoveProject(projectId);
        return count;
    }
    public async Task<int> DeleteAllAsync()
    {
        var count = await Server.DeleteAllAsync();
        new TrackingSessionStore(_dataPath).Clear();
        return count;
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_server is not null)
            {
                Stopping?.Invoke(this, EventArgs.Empty);
                _server.AuthorizationChanged -= OnAuthorizationChanged;
                _server.ConnectionChanged -= OnConnectionChanged;
                _server.WorkflowChanged -= OnWorkflowChanged;
                _server.DailyActivityChanged -= OnDailyActivityChanged;
                await _server.DisposeAsync().ConfigureAwait(false);
                _server = null;
                UpdateConnectionState();
                await WriteLogAsync("server stopped").ConfigureAwait(false);
            }
        }
        finally { _gate.Release(); }
        _gate.Dispose();
    }

    private LocalServer Server => _server ?? throw new InvalidOperationException("로컬 서버가 실행 중이 아닙니다.");

    private async Task WriteLogAsync(string message)
    {
        try { await _log.WriteAsync(message); }
        catch { }
    }

    private void OnAuthorizationChanged(object? sender, EventArgs eventArgs)
    {
        _isCodexAuthorized = _server?.HasAuthorizedCodex ?? _isCodexAuthorized;
        AuthorizationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnConnectionChanged(object? sender, EventArgs eventArgs)
    {
        UpdateConnectionState();
        AuthorizationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnWorkflowChanged(object? sender, WorkflowChangedEventArgs eventArgs)
        => WorkflowChanged?.Invoke(this, eventArgs);

    private void OnDailyActivityChanged(object? sender, DailyActivityChangedEventArgs eventArgs)
        => DailyActivityChanged?.Invoke(this, eventArgs);

    private void UpdateConnectionState()
    {
        _isCodexConnected = _server?.HasActiveMcpClient ?? false;
        _lastMcpActivityAt = _server?.LastMcpActivityAt;
    }

    private static string ResolveDataPath(string? path)
        => path ?? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMSR");
}
