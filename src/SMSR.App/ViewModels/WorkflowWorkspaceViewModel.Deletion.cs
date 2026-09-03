namespace SMSR.App.ViewModels;

public sealed partial class WorkflowWorkspaceViewModel
{
    private async Task DeleteWorkflowAsync()
    {
        var projectId = Selection.ProjectId;
        var workflowId = Selection.WorkflowId;
        if (!_platform.Confirm("워크플로우 기록 삭제", $"'{projectId}'의 선택한 작업 기록을 삭제할까요?\n\nSQLite 이력, 활동 JSONL, 자동추적 연결이 삭제됩니다. 내보낸 ZIP/HTML은 유지됩니다.")) return;
        await DeleteAsync(() => _host.DeleteWorkflowAsync(projectId, workflowId), "선택한 작업 기록");
    }

    private async Task DeleteProjectAsync()
    {
        var projectId = Selection.ProjectId;
        if (!_platform.Confirm("프로젝트 기록 삭제", $"'{projectId}'의 모든 작업 기록을 삭제할까요?\n\nSQLite 이력, 활동 JSONL, 자동추적 연결이 삭제됩니다. 내보낸 ZIP/HTML은 유지됩니다.")) return;
        await DeleteAsync(() => _host.DeleteProjectAsync(projectId), $"{projectId} 프로젝트 기록");
    }

    private async Task DeleteAllAsync()
    {
        if (!_platform.Confirm("전체 기록 삭제", "모든 프로젝트의 작업 기록을 삭제할까요?\n\nSQLite 이력, 활동 JSONL, 자동추적 연결이 삭제됩니다. 내보낸 ZIP/HTML과 앱 설정은 유지됩니다.")) return;
        await DeleteAsync(_host.DeleteAllAsync, "전체 작업 기록");
    }

    private async Task DeleteAsync(Func<Task<int>> action, string label)
    {
        _isDeleting = true;
        NotifyCommandStates();
        try
        {
            Monitor.Clear();
            var count = await action();
            Selection.Reset();
            await Selection.LoadAsync();
            if (_host.IsRunning && !string.IsNullOrWhiteSpace(Selection.ProjectId)
                && !string.IsNullOrWhiteSpace(Selection.WorkflowId)) await RefreshMonitorAsync();
            StatusMessage = $"{label}을 삭제했습니다. 삭제된 워크플로우: {count}개";
        }
        catch { StatusMessage = $"{label}을 삭제하지 못했습니다."; }
        finally { _isDeleting = false; NotifyCommandStates(); }
    }
}
