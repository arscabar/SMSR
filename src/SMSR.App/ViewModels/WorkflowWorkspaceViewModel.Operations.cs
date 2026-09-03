using System.ComponentModel;

namespace SMSR.App.ViewModels;

public sealed partial class WorkflowWorkspaceViewModel
{
    private async Task RefreshSelectionAsync()
    {
        try
        {
            await Selection.LoadAsync();
            StatusMessage = Selection.ProjectIds.Count == 0
                ? "저장된 프로젝트가 없습니다. ID를 직접 입력하세요." : "저장 목록을 새로 고쳤습니다.";
        }
        catch { StatusMessage = "저장된 목록을 읽지 못했습니다."; }
    }

    private async Task RefreshMonitorAsync()
    {
        try
        {
            await Monitor.RefreshAsync(Selection.ProjectId, Selection.WorkflowId);
            Monitor.StartLiveUpdates(Selection.ProjectId, Selection.WorkflowId);
            StatusMessage = "워크플로우 상태를 새로 고쳤습니다.";
        }
        catch { StatusMessage = "워크플로우 상태를 읽지 못했습니다."; }
    }

    private async Task GenerateSummaryAsync()
    {
        try { await Monitor.GenerateSummaryAsync(Selection.ProjectId, Selection.WorkflowId); StatusMessage = "요약을 생성해 저장했습니다."; }
        catch { StatusMessage = "요약을 생성하지 못했습니다."; }
    }

    private async Task ExportAsync()
    {
        try { StatusMessage = $"내보내기를 완료했습니다: {(await Monitor.ExportAsync(Selection.ProjectId, Selection.WorkflowId)).DirectoryPath}"; }
        catch { StatusMessage = "내보내기를 완료하지 못했습니다."; }
    }

    private void OpenDashboard()
    {
        var url = $"{_host.Address}/dashboard?projectId={Uri.EscapeDataString(Selection.ProjectId.Trim())}&workflowId={Uri.EscapeDataString(Selection.WorkflowId.Trim())}";
        StatusMessage = _platform.TryOpenBrowser(url) ? "기본 브라우저에서 대시보드를 열었습니다." : "대시보드를 열지 못했습니다.";
    }

    private void OnSelectionChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName is nameof(WorkflowSelectionViewModel.ProjectId)
            or nameof(WorkflowSelectionViewModel.WorkflowId)) NotifyCommandStates();
    }

    private void OnHostStateChanged(object? sender, EventArgs eventArgs)
    {
        if (!_host.IsRunning) Monitor.StopLiveUpdates();
        NotifyCommandStates();
    }

    private void OnHostStopping(object? sender, EventArgs eventArgs) => Monitor.StopLiveUpdates();
}
