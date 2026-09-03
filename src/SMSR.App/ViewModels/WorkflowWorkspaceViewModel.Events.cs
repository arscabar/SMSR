using SMSR.App.Mvp;

namespace SMSR.App.ViewModels;

public sealed partial class WorkflowWorkspaceViewModel
{
    private void OnWorkflowChanged(object? sender, WorkflowChangedEventArgs eventArgs)
    {
        if (_isDeleting) return;
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null) return;
        _ = dispatcher.InvokeAsync(() => _ = HandleWorkflowChangedAsync(eventArgs));
    }

    private async Task HandleWorkflowChangedAsync(WorkflowChangedEventArgs eventArgs)
    {
        try
        {
            await Selection.ReloadCalendarAsync();
            if (HasWorkflowSelection())
            {
                if (Selection.ProjectId != eventArgs.ProjectId || Selection.WorkflowId != eventArgs.WorkflowId)
                    StatusMessage = $"다른 작업이 갱신되었습니다: {eventArgs.ProjectId}. 캘린더에서 선택할 수 있습니다.";
                return;
            }
            await Selection.SelectAsync(eventArgs.ProjectId, eventArgs.WorkflowId);
            await RefreshMonitorAsync();
        }
        catch { StatusMessage = "갱신된 그래프 목록을 불러오지 못했습니다."; }
    }

    internal async Task SelectCalendarWorkflowAsync(WorkflowChoice choice)
    {
        try
        {
            await Selection.SelectAsync(choice.ProjectId, choice.WorkflowId);
            await RefreshMonitorAsync();
            StatusMessage = $"{choice.DateTimeLabel}의 작업을 열었습니다: {choice.Title}";
        }
        catch { StatusMessage = "선택한 작업을 불러오지 못했습니다."; }
    }
}
