namespace SMSR.App.ViewModels;

public sealed record SummaryProjectScopeOption(string Label, string? ProjectId);

public sealed partial class WorkflowWorkspaceViewModel
{
    private void RefreshSummaryProjectScopes(IEnumerable<string> projectIds)
    {
        var selectedId = _summaryScopeInitialized ? SummaryProjectScope.ProjectId : Selection.ProjectId;
        var selectedAll = _summaryScopeInitialized && IsAllSummaryProjects;
        SummaryProjectScopes.Clear();
        SummaryProjectScopes.Add(new(AllProjectsSummaryScope, null));
        foreach (var projectId in projectIds.Distinct(StringComparer.Ordinal))
            SummaryProjectScopes.Add(new(projectId, projectId));

        SummaryProjectScope = selectedAll ? SummaryProjectScopes[0]
            : SummaryProjectScopes.FirstOrDefault(item => item.ProjectId == selectedId)
                ?? SummaryProjectScopes[0];
        _summaryScopeInitialized = true;
    }

    private bool IncludesSummaryProject(string projectId)
        => IsAllSummaryProjects || projectId == SummaryProjectScope.ProjectId;
}
