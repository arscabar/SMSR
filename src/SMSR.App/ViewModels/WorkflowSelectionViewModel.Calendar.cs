using System.Collections.ObjectModel;

namespace SMSR.App.ViewModels;

public sealed partial class WorkflowSelectionViewModel
{
    private readonly List<WorkflowChoice> _calendarSource = [];
    private readonly List<DailyActivityItem> _dailyCalendarSource = [];
    private DateTime? _selectedDate;
    private DateTime _displayMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private CalendarDayItem? _selectedCalendarDay;
    private WorkflowChoice? _selectedCalendarWorkflow;
    private DailyActivityItem? _selectedDailyActivity;

    public ObservableCollection<WorkflowChoice> CalendarWorkflows { get; } = [];
    public ObservableCollection<CalendarDayItem> CalendarDays { get; } = [];
    public event Action<WorkflowChoice>? WorkflowRequested;
    public event Action<DailyActivityItem>? DailyActivityRequested;

    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (!SetField(ref _selectedDate, value?.Date)) return;
            _selectedCalendarDay = CalendarDays.FirstOrDefault(item => item.Date == _selectedDate);
            OnPropertyChanged(nameof(SelectedCalendarDay));
            FilterCalendar(true);
        }
    }

    public CalendarDayItem? SelectedCalendarDay
    {
        get => _selectedCalendarDay;
        set
        {
            if (!SetField(ref _selectedCalendarDay, value) || value?.Date is null) return;
            SelectedDate = value.Date;
        }
    }

    public string DisplayMonthLabel => $"{_displayMonth:yyyy년 M월}";

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

}
