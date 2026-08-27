using SMSR.App.Mvp;

namespace SMSR.App.Services;

public sealed class LocalServerHost(string? dataPath = null) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private LocalServer? _server;

    public event EventHandler? StateChanged;
    public bool IsRunning => _server is not null;
    public string Address => _server?.Address ?? "";
    public string Token => _server?.Token ?? "";

    public async Task StartAsync()
    {
        await _gate.WaitAsync();
        try { _server ??= await LocalServer.StartAsync(dataPath); }
        finally { _gate.Release(); }
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task StopAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_server is not null) await _server.DisposeAsync();
            _server = null;
        }
        finally { _gate.Release(); }
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public Task<IReadOnlyList<string>> GetProjectIdsAsync() => Server.GetProjectIdsAsync();
    public Task<IReadOnlyList<string>> GetWorkflowIdsAsync(string projectId) => Server.GetWorkflowIdsAsync(projectId);
    public Task<WorkflowState> GetStateAsync(string projectId, string workflowId) => Server.GetStateAsync(projectId, workflowId);
    public Task<IReadOnlyList<RecentEvent>> GetRecentEventsAsync(string projectId, string workflowId) => Server.GetRecentEventsAsync(projectId, workflowId);
    public Task<WorkflowSummary?> GetLatestSummaryAsync(string projectId, string workflowId) => Server.GetLatestSummaryAsync(projectId, workflowId);
    public Task<WorkflowSummary> GenerateSummaryAsync(string projectId, string workflowId) => Server.GenerateSummaryAsync(projectId, workflowId);
    public Task<ExportResult> ExportAsync(string projectId, string workflowId) => Server.ExportAsync(projectId, workflowId);

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _gate.Dispose();
    }

    private LocalServer Server => _server ?? throw new InvalidOperationException("로컬 서버가 실행 중이 아닙니다.");
}
