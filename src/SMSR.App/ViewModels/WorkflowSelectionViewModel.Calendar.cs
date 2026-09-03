using System.Collections.ObjectModel;

namespace SMSR.App.ViewModels;

public sealed partial class WorkflowSelectionViewModel
{
    private readonly List<WorkflowChoice> _calendarSource = [];
    private DateTime? _selectedDate;
    private WorkflowChoice? _selectedCalendarWorkflow;

    public ObservableCollection<WorkflowChoice> CalendarWorkflows { get; } = [];
    public event Action<WorkflowChoice>? WorkflowRequested;

    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (!SetField(ref _selectedDate, value?.Date)) return;
            FilterCalendar(true);
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
        : $"{SelectedDate:yyyy년 M월 d일} · {CalendarWorkflows.Count}개 작업";

    private async Task LoadCalendarAsync()
    {
        _calendarSource.Clear();
        foreach (var entry in await server.GetWorkflowCalendarAsync())
            _calendarSource.Add(new(entry.ProjectId, entry.WorkflowId,
                string.IsNullOrWhiteSpace(entry.Title) ? "이름 없는 이전 작업" : entry.Title,
                entry.Status, entry.NodeCount, entry.UpdatedAtUtc));
        _calendarSource.Sort((left, right) => Nullable.Compare(right.UpdatedAtUtc, left.UpdatedAtUtc));
        if (SelectedDate is null)
        {
            _selectedDate = _calendarSource.FirstOrDefault(item => item.ActivityDate is not null)?.ActivityDate
                ?? DateTime.Today;
            OnPropertyChanged(nameof(SelectedDate));
        }
        FilterCalendar();
    }

    private void FilterCalendar(bool selectFirst = false)
    {
        SelectedCalendarWorkflow = null;
        CalendarWorkflows.Clear();
        foreach (var item in _calendarSource.Where(item => item.ActivityDate == SelectedDate).Take(200))
            CalendarWorkflows.Add(item);
        OnPropertyChanged(nameof(CalendarSummary));
        if (selectFirst && CalendarWorkflows.Count > 0)
            SelectedCalendarWorkflow = CalendarWorkflows[0];
    }
}
