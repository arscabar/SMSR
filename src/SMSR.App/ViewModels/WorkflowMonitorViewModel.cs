using System.Collections.ObjectModel;
using SMSR.App.Mvp;
using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed class WorkflowMonitorViewModel(LocalServerHost host) : ViewModelBase
{
    private string _summary = "선택한 워크플로우의 상태를 조회하세요.";
    public ObservableCollection<StateNode> Nodes { get; } = [];
    public ObservableCollection<RecentEvent> RecentEvents { get; } = [];
    public string Summary { get => _summary; private set => SetField(ref _summary, value); }

    public async Task RefreshAsync(string projectId, string workflowId)
    {
        var state = await host.GetStateAsync(projectId, workflowId);
        var events = await host.GetRecentEventsAsync(projectId, workflowId);
        Nodes.Clear();
        RecentEvents.Clear();
        foreach (var node in state.Nodes) Nodes.Add(node);
        foreach (var item in events) RecentEvents.Add(item);
        Summary = (await host.GetLatestSummaryAsync(projectId, workflowId))?.Content ?? "저장된 요약이 없습니다.";
    }

    public async Task GenerateSummaryAsync(string projectId, string workflowId)
        => Summary = (await host.GenerateSummaryAsync(projectId, workflowId)).Content;

    public Task<ExportResult> ExportAsync(string projectId, string workflowId)
        => host.ExportAsync(projectId, workflowId);
}
