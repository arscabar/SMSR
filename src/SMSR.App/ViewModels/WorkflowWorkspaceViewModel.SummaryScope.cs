namespace SMSR.App.ViewModels;

public sealed record SummaryProjectScopeOption(string Label, string? ProjectId)
{
    public override string ToString() => Label;
}

public sealed partial class WorkflowWorkspaceViewModel
{
    public async Task RefreshSummaryProjectScopesAsync()
    {
        if (Selection.SelectedDate is not { } endDate) return;
        var version = ++_summaryScopeRefreshVersion;
        var startDate = Selection.SummaryStartDate ?? endDate;
        var (start, _) = LocalDayRange(startDate);
        var (_, end) = LocalDayRange(endDate);
        var workflowsTask = _host.GetWorkflowCalendarAsync();
        var activitiesTask = _host.GetDailyActivitiesAsync(start, end);
        await Task.WhenAll(workflowsTask, activitiesTask);
        if (version != _summaryScopeRefreshVersion) return;
        var projectIds = workflowsTask.Result
            .Where(item => item.UpdatedAtUtc?.ToLocalTime().Date is { } date
                && date >= startDate.Date && date <= endDate.Date)
            .Select(item => item.ProjectId)
            .Concat(activitiesTask.Result.Select(item => item.ProjectId));
        var selectedId = _summaryScopeInitialized ? SummaryProjectScope.ProjectId : Selection.ProjectId;
        var selectedAll = _summaryScopeInitialized && IsAllSummaryProjects;
        SummaryProjectScopes.Clear();
        SummaryProjectScopes.Add(new(AllProjectsSummaryScope, null));
        foreach (var projectId in projectIds.Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal))
            SummaryProjectScopes.Add(new(projectId, projectId));

        SummaryProjectScope = selectedAll ? SummaryProjectScopes[0]
            : SummaryProjectScopes.FirstOrDefault(item => item.ProjectId == selectedId)
                ?? SummaryProjectScopes[0];
        _summaryScopeInitialized = true;
    }

    private bool IncludesSummaryProject(string projectId)
        => IsAllSummaryProjects || projectId == SummaryProjectScope.ProjectId;
}
