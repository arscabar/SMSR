namespace SMSR.App.ViewModels;

public sealed record CalendarDayItem(DateTime? Date, int GraphCount, int ActivityCount)
{
    public bool IsInMonth => Date is not null;
    public bool IsToday => Date == DateTime.Today;
    public bool HasGraphs => GraphCount > 0;
    public bool HasActivities => ActivityCount > 0;
    public string DayLabel => Date?.Day.ToString() ?? "";
    public string GraphLabel => HasGraphs ? $"그래프 {GraphCount}" : "";
    public string ActivityLabel => HasActivities ? $"기록 {ActivityCount}" : "";
    public string AccessibleLabel => Date is null ? "빈 날짜"
        : $"{Date:yyyy년 M월 d일}, 그래프 {GraphCount}개, 작업 기록 {ActivityCount}개";
}
