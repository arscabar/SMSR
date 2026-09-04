using System.Collections.ObjectModel;

namespace SMSR.App.ViewModels;

public sealed partial class WorkflowSelectionViewModel
{
    private readonly List<WorkflowChoice> _calendarSource = [];
    private DateTime? _selectedDate;
    private WorkflowChoice? _selectedCalendarWorkflow;
    private DailyActivityItem? _selectedDailyActivity;

    public ObservableCollection<WorkflowChoice> CalendarWorkflows { get; } = [];
    public event Action<WorkflowChoice>? WorkflowRequested;
    public event Action<DailyActivityItem>? DailyActivityRequested;

    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (!SetField(ref _selectedDate, value?.Date)) return;
            FilterCalendar(true);
            if (value is not null) _ = LoadDailyActivitiesAsync(value.Value.Date);
        }
    }

    public DailyActivityItem? SelectedDailyActivity
    {
        get => _selectedDailyActivity;
        set
        {
            if (!SetField(ref _selectedDailyActivity, value) || string.IsNullOrWhiteSpace(value?.WorkflowId)) return;
            DailyActivityRequested?.Invoke(value);
        }
    }

    public WorkflowChoice? SelectedCalendarWorkflow
    {
        get => _selectedCalendarWorkflow;
        set
        {
            if (!SetField(ref _selectedCalendarWorkflow, value) || value is null) return;
            WorkflowRequested?.Invoke(value);
        }
    }

    public string CalendarSummary => SelectedDate is null
        ? "날짜를 선택하세요."
        : $"{SelectedDate:yyyy년 M월 d일} · 그래프 {CalendarWorkflows.Count}개 · 작업 기록 {DailyActivities.Count}개";
    public string DailyOverview => DailyActivities.Count == 0 ? "이 날짜에는 프로젝트 변경 기록이 없습니다."
        : $"프로젝트 {DailyActivities.Select(item => item.ProjectId).Distinct().Count()}개 · 완료 {DailyActivities.Count(item => item.Status == "SUCCESS")}건 · 문제 {DailyActivities.Count(item => item.Status != "SUCCESS")}건 · 변경 파일 {DailyActivities.SelectMany(item => item.Files).Distinct().Count()}개";

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
                .Max()?.ToLocalTime().Date
                ?? DateTime.Today;
            OnPropertyChanged(nameof(SelectedDate));
        }
        FilterCalendar();
        if (SelectedDate is { } selectedDate) await LoadDailyActivitiesAsync(selectedDate);
    }

    private void FilterCalendar(bool selectFirst = false)
    {
        SelectedCalendarWorkflow = null;
        SelectedDailyActivity = null;
        CalendarWorkflows.Clear();
        DailyActivities.Clear();
        foreach (var item in _calendarSource.Where(item => item.ActivityDate == SelectedDate).Take(200))
            CalendarWorkflows.Add(item);
        OnPropertyChanged(nameof(CalendarSummary));
        OnPropertyChanged(nameof(DailyOverview));
        if (selectFirst && CalendarWorkflows.Count > 0)
            SelectedCalendarWorkflow = CalendarWorkflows[0];
    }

    private async Task LoadDailyActivitiesAsync(DateTime date)
    {
        var unspecified = DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified);
        var start = new DateTimeOffset(unspecified, TimeZoneInfo.Local.GetUtcOffset(unspecified)).ToUniversalTime();
        var endDate = unspecified.AddDays(1);
        var end = new DateTimeOffset(endDate, TimeZoneInfo.Local.GetUtcOffset(endDate)).ToUniversalTime();
        var items = await server.GetDailyActivitiesAsync(start, end);
        if (SelectedDate != date.Date) return;
        SelectedDailyActivity = null;
        DailyActivities.Clear();
        foreach (var item in items) DailyActivities.Add(DailyActivityItem.From(item));
        OnPropertyChanged(nameof(CalendarSummary));
        OnPropertyChanged(nameof(DailyOverview));
    }
}
