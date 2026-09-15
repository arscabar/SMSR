namespace SMSR.App.ViewModels;

public sealed partial class WorkflowSelectionViewModel
{
    private async Task LoadCalendarAsync()
    {
        _calendarSource.Clear();
        foreach (var entry in await server.GetWorkflowCalendarAsync())
            _calendarSource.Add(new(entry.ProjectId, entry.WorkflowId,
                string.IsNullOrWhiteSpace(entry.Title) ? "이름 없는 이전 작업" : entry.Title,
                entry.Status, entry.NodeCount, entry.UpdatedAtUtc));
        _calendarSource.Sort((left, right) => Nullable.Compare(right.UpdatedAtUtc, left.UpdatedAtUtc));
        var latestDaily = await server.GetLatestDailyActivityAtAsync();
        if (SelectedDate is null)
        {
            var latestGraph = _calendarSource.FirstOrDefault(item => item.ActivityDate is not null)?.UpdatedAtUtc;
            _selectedDate = new[] { latestGraph, latestDaily }.Where(value => value is not null)
                .Max()?.ToLocalTime().Date ?? DateTime.Today;
            OnPropertyChanged(nameof(SelectedDate));
        }
        var selectedDate = SelectedDate ?? DateTime.Today;
        _displayMonth = new(selectedDate.Year, selectedDate.Month, 1);
        await LoadMonthActivitiesAsync();
        BuildMonthGrid();
        FilterCalendar();
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
        ProjectIds.Clear();
        foreach (var projectId in CalendarWorkflows.Select(item => item.ProjectId).Distinct(StringComparer.OrdinalIgnoreCase))
            ProjectIds.Add(projectId);
        if (!ProjectIds.Contains(currentProject)) ProjectId = ProjectIds.FirstOrDefault() ?? "";
        else FilterProjectWorkflows();
        foreach (var item in _dailyCalendarSource
                     .Where(item => item.RecordedAtUtc.ToLocalTime().Date == SelectedDate).Take(200))
            DailyActivities.Add(item);
        OnPropertyChanged(nameof(CalendarSummary));
        OnPropertyChanged(nameof(DailyOverview));
    }

    private void FilterProjectWorkflows()
    {
        WorkflowIds.Clear();
        Workflows.Clear();
        foreach (var item in CalendarWorkflows.Where(item => item.ProjectId == ProjectId))
        {
            WorkflowIds.Add(item.WorkflowId);
            Workflows.Add(item);
        }
        OnPropertyChanged(nameof(SelectedWorkflow));
        if (!WorkflowIds.Contains(WorkflowId)) WorkflowId = WorkflowIds.FirstOrDefault() ?? "";
    }
}
