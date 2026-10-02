using SMSR.App.ViewModels;

namespace SMSR.App.Mvp;

internal static class WorkflowSelectionSelfCheck
{
    internal static async Task RunAsync(WorkflowSelectionViewModel selection, EventStore store)
    {
        if (selection.SelectedDate != DateTime.Today || !selection.DailyActivities.Any(a => a.ActivityId == "daily-simple"))
            throw new Exception("Today activities missing on initial load");
        await selection.SelectAsync("demo", "wf-1");
        // WPF writes null to SelectedItem while its collection is cleared.
        System.Collections.Specialized.NotifyCollectionChangedEventHandler binding = (_, _) => selection.SelectedWorkflow = null;
        selection.Workflows.CollectionChanged += binding;
        try
        {
            await store.RecordDailyActivityAsync(new("daily-refresh", "other-project", "refresh-test", "갱신 검증", "시험 기록"));
            await Task.WhenAll(selection.ReloadCalendarAsync(), selection.ReloadCalendarAsync());
            if (!selection.DailyActivities.Any(a => a.ActivityId == "daily-refresh"))
                throw new Exception("Today activities require a second date click");
            Check();
            await selection.LoadAsync();
            Check();
            selection.SelectedDate = DateTime.Today.AddDays(-1);
            Check();
            selection.SelectedDate = DateTime.Today;
            Check();
        }
        finally { selection.Workflows.CollectionChanged -= binding; }
        WorkflowWheelSelfCheck.Run();
        void Check()
        {
            if (selection.ProjectId != "demo" || selection.WorkflowId != "wf-1"
                || selection.SelectedWorkflow?.WorkflowId != "wf-1"
                || selection.Workflows.Select(w => w.WorkflowId).Distinct().Count() != selection.Workflows.Count)
                throw new Exception("Refresh/date filter replaced the explicit workflow selection");
        }
    }
}
