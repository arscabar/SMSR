using SMSR.App.Infrastructure;
using SMSR.App.Services;
using SMSR.App.ViewModels;
using SMSR.App.Views;

namespace SMSR.App.Mvp;

internal static class GraphClientPreview
{
    internal static async Task SeedAsync(EventStore store)
    {
        for (var i = 0; i < 25; i++)
            foreach (var project in new[] { "QualityTest", $"ClientProject-{i:00}" })
            {
                var workflow = $"client-{i:00}";
                await store.SavePlanAsync(project, workflow, [new("node", $"선택 유지 시험 {i:00}")]);
                await store.RecordAsync(new($"{project}-{i}", project, workflow, "node", "test", "NODE_STATUS_CHANGED", "SUCCESS", null, null, null, null));
            }
        await store.RecordDailyActivityAsync(new("client-today", "QualityTest", "test", "오늘 기록 시험", "초기 표시 확인"));
    }
    internal static async Task<System.Windows.Window> OpenAsync(LocalServerHost host)
    {
        var model = new WorkflowWorkspaceViewModel(host, new WindowsPlatformActions(), new AppSettingsService(host.DataPath));
        await model.LoadAsync();
        await model.Selection.SelectAsync("QualityTest", "client-00");
        var window = new System.Windows.Window { Title = "SMSR — 클라이언트 수정 시험", Width = 580, Height = 760,
            Content = new WorkflowPanel { DataContext = model }, Background = System.Windows.Media.Brushes.Black };
        window.Show(); return window;
    }
}
