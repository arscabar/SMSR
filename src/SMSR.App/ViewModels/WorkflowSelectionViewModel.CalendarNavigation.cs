namespace SMSR.App.ViewModels;

public sealed partial class WorkflowSelectionViewModel
{
    private void BuildMonthGrid()
    {
        CalendarDays.Clear();
        for (var index = 0; index < (int)_displayMonth.DayOfWeek; index++)
            CalendarDays.Add(new(null, 0, 0));
        for (var day = 1; day <= DateTime.DaysInMonth(_displayMonth.Year, _displayMonth.Month); day++)
        {
            var date = new DateTime(_displayMonth.Year, _displayMonth.Month, day);
            var graphs = _calendarSource.Count(item => item.ActivityDate == date);
            var activities = _dailyCalendarSource.Count(item => item.RecordedAtUtc.ToLocalTime().Date == date);
            CalendarDays.Add(new(date, graphs, activities));
        }
        while (CalendarDays.Count % 7 != 0) CalendarDays.Add(new(null, 0, 0));
        _selectedCalendarDay = CalendarDays.FirstOrDefault(item => item.Date == SelectedDate);
        OnPropertyChanged(nameof(SelectedCalendarDay));
        OnPropertyChanged(nameof(DisplayMonthLabel));
    }

    private async Task MoveMonthAsync(int months)
    {
        _displayMonth = _displayMonth.AddMonths(months);
        SelectedDate = IsCurrentMonth(_displayMonth) ? DateTime.Today : _displayMonth;
        await LoadMonthActivitiesAsync();
        BuildMonthGrid();
        FilterCalendar();
    }

    private async Task ShowTodayAsync()
    {
        _displayMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        SelectedDate = DateTime.Today;
        await LoadMonthActivitiesAsync();
        BuildMonthGrid();
        FilterCalendar();
    }

    private static bool IsCurrentMonth(DateTime month)
        => month.Year == DateTime.Today.Year && month.Month == DateTime.Today.Month;
}
