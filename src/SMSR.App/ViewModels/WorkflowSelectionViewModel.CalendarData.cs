namespace SMSR.App.ViewModels;

public sealed partial class WorkflowSelectionViewModel
{
    private readonly SemaphoreSlim _calendarGate = new(1, 1);
    private bool _updatingCatalog;
    private async Task LoadCalendarAsync()
    {
        await _calendarGate.WaitAsync();
        try
        {
        var entries = await server.GetWorkflowCalendarAsync();
        _calendarSource.Clear();
        foreach (var entry in entries)
            _calendarSource.Add(new(entry.ProjectId, entry.WorkflowId,
                string.IsNullOrWhiteSpace(entry.Title) ? "이름 없는 이전 작업" : entry.Title,
                entry.Status, entry.NodeCount, entry.UpdatedAtUtc));
        _calendarSource.Sort((left, right) => Nullable.Compare(right.UpdatedAtUtc, left.UpdatedAtUtc));
        if (SelectedDate is null)
        {
            _selectedDate = DateTime.Today;
            OnPropertyChanged(nameof(SelectedDate));
        }
        var selectedDate = SelectedDate ?? DateTime.Today;
        _displayMonth = new(selectedDate.Year, selectedDate.Month, 1);
        await LoadMonthActivitiesAsync();
        BuildMonthGrid();
        FilterCalendar();
        }
        finally { _calendarGate.Release(); }
    }

    private async Task LoadMonthActivitiesAsync()
    {
        var localStart = DateTime.SpecifyKind(_displayMonth, DateTimeKind.Unspecified);
        var localEnd = localStart.AddMonths(1);
        var start = new DateTimeOffset(localStart, TimeZoneInfo.Local.GetUtcOffset(localStart)).ToUniversalTime();
        var end = new DateTimeOffset(localEnd, TimeZoneInfo.Local.GetUtcOffset(localEnd)).ToUniversalTime();
        var items = await server.GetDailyActivitiesAsync(start, end);
        _dailyCalendarSource.Clear();
        _dailyCalendarSource.AddRange(items.Select(DailyActivityItem.From));
    }

    private void FilterCalendar()
    {
        SelectedCalendarWorkflow = null;
        SelectedDailyActivity = null;
        CalendarWorkflows.Clear();
        DailyActivities.Clear();
        var startDate = SummaryStartDate ?? SelectedDate;
        foreach (var item in _calendarSource
                     .Where(item => item.ActivityDate is { } date && date >= startDate && date <= SelectedDate)
                     .GroupBy(item => (item.ProjectId, item.WorkflowId)).Select(group => group.First()).Take(200))
            CalendarWorkflows.Add(item);
        var currentProject = ProjectId;
        foreach (var projectId in _calendarSource.Select(item => item.ProjectId).Append(currentProject)
                     .Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase))
            if (!ProjectIds.Contains(projectId)) ProjectIds.Add(projectId);
        FilterProjectWorkflows();
        foreach (var item in _dailyCalendarSource
                     .Where(item => item.RecordedAtUtc.ToLocalTime().Date == SelectedDate).Take(200))
            DailyActivities.Add(item);
        OnPropertyChanged(nameof(CalendarSummary));
        OnPropertyChanged(nameof(DailyOverview));
    }

    private void FilterProjectWorkflows()
    {
        _updatingCatalog = true;
        try
        {
        var selected = SelectedWorkflow;
        WorkflowIds.Clear();
        Workflows.Clear();
        foreach (var item in CalendarWorkflows.Where(item => item.ProjectId == ProjectId))
        {
            WorkflowIds.Add(item.WorkflowId);
            Workflows.Add(item);
        }
        if (selected is not null && selected.ProjectId == ProjectId && !WorkflowIds.Contains(selected.WorkflowId))
        {
            WorkflowIds.Add(selected.WorkflowId);
            Workflows.Add(selected);
        }
        }
        finally { _updatingCatalog = false; }
        OnPropertyChanged(nameof(SelectedWorkflow));
        if (string.IsNullOrWhiteSpace(WorkflowId)) WorkflowId = WorkflowIds.FirstOrDefault() ?? "";
    }
}
