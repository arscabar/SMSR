using SMSR.App.Mvp;

namespace SMSR.App.Services;

public sealed class LocalServerHost(string? dataPath = null) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _dataPath = ResolveDataPath(dataPath);
    private readonly LocalActivityLog _log = new(System.IO.Path.Combine(ResolveDataPath(dataPath), "logs"));
    private LocalServer? _server;

    public event EventHandler? StateChanged;
    public bool IsRunning => _server is not null;
    public string Address => _server?.Address ?? "";
    public string Token => _server?.Token ?? "";
    public string LogPath => _log.Path;
    public string DataPath => _dataPath;

    public async Task StartAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_server is null)
            {
                _server = await LocalServer.StartAsync(_dataPath);
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
                await _server.DisposeAsync();
                await WriteLogAsync("server stopped");
            }
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
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_server is not null)
            {
                await _server.DisposeAsync().ConfigureAwait(false);
                _server = null;
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

    private static string ResolveDataPath(string? path)
        => path ?? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMSR");
}
