using System.Collections.ObjectModel;
using System.Windows.Threading;
using SMSR.App.Mvp;
using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed class WorkflowMonitorViewModel : ViewModelBase
{
    private readonly LocalServerHost _host;
    private readonly DispatcherTimer _pollTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private CancellationTokenSource? _streamCancellation;
    private string _summary = "선택한 워크플로우의 상태를 조회하세요.";
    private string _updateMode = "수동 새로 고침";

    public WorkflowMonitorViewModel(LocalServerHost host)
    {
        _host = host;
        _pollTimer.Tick += async (_, _) => await PollAsync();
    }

    public ObservableCollection<StateNode> Nodes { get; } = [];
    public ObservableCollection<RecentEvent> RecentEvents { get; } = [];
    public string Summary { get => _summary; private set => SetField(ref _summary, value); }
    public string UpdateMode { get => _updateMode; private set => SetField(ref _updateMode, value); }

    public async Task RefreshAsync(string projectId, string workflowId)
    {
        _projectId = projectId;
        _workflowId = workflowId;
        var state = await _host.GetStateAsync(projectId, workflowId);
        var events = await _host.GetRecentEventsAsync(projectId, workflowId);
        Nodes.Clear();
        RecentEvents.Clear();
        foreach (var node in state.Nodes) Nodes.Add(node);
        foreach (var item in events) RecentEvents.Add(item);
        Summary = (await _host.GetLatestSummaryAsync(projectId, workflowId))?.Content ?? "저장된 요약이 없습니다.";
    }

    public async Task GenerateSummaryAsync(string projectId, string workflowId)
        => Summary = (await _host.GenerateSummaryAsync(projectId, workflowId)).Content;

    public Task<ExportResult> ExportAsync(string projectId, string workflowId)
        => _host.ExportAsync(projectId, workflowId);

    public void StartLiveUpdates(string projectId, string workflowId)
    {
        StopLiveUpdates();
        _projectId = projectId;
        _workflowId = workflowId;
        _pollTimer.Stop();
        var cancellation = new CancellationTokenSource();
        _streamCancellation = cancellation;
        _ = ListenAsync(projectId, workflowId, cancellation);
    }

    public void StopLiveUpdates()
    {
        _streamCancellation?.Cancel();
        _streamCancellation = null;
        _pollTimer.Stop();
        UpdateMode = "수동 새로 고침";
    }

    private async Task ListenAsync(string projectId, string workflowId, CancellationTokenSource cancellation)
    {
        try
        {
            UpdateMode = "SSE 실시간 갱신";
            var url = $"{_host.Address}/api/events/stream?projectId={Uri.EscapeDataString(projectId)}&workflowId={Uri.EscapeDataString(workflowId)}";
            await SseStateClient.ListenAsync(url, () => RefreshAsync(projectId, workflowId), cancellation.Token);
        }
        catch (OperationCanceledException) { return; }
        catch { }
        finally
        {
            if (_streamCancellation == cancellation)
            {
                _streamCancellation = null;
                UpdateMode = "SSE 연결 해제 · 2초 polling";
                _pollTimer.Start();
            }
            cancellation.Dispose();
        }
    }

    private async Task PollAsync()
    {
        try { await RefreshAsync(_projectId, _workflowId); }
        catch { }
    }

    private string _projectId = "";
    private string _workflowId = "";
}
