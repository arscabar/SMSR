using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SMSR.App.ViewModels;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace SMSR.App.Views;

public partial class WorkflowPanel : WpfUserControl
{
    public WorkflowPanel() => InitializeComponent();

    private async void OpenSummaryPopup_Click(object sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is WorkflowWorkspaceViewModel workspace)
            await workspace.RefreshSummaryProjectScopesAsync();
        SummaryOverlay.Visibility = Visibility.Visible;
        SummaryQuestionTextBox.Focus();
    }

    private void CloseSummaryPopup_Click(object sender, RoutedEventArgs eventArgs)
        => SummaryOverlay.Visibility = Visibility.Collapsed;

    private void WorkflowRoot_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs eventArgs)
    {
        if (eventArgs.Key != Key.Escape || SummaryOverlay.Visibility != Visibility.Visible) return;
        SummaryOverlay.Visibility = Visibility.Collapsed;
        eventArgs.Handled = true;
    }

    private void Calendar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs eventArgs)
    {
        if (sender is not System.Windows.Controls.ListBox
            || FindParent<ListBoxItem>(eventArgs.OriginalSource as DependencyObject)?.DataContext
                is not CalendarDayItem { Date: { } date }
            || DataContext is not WorkflowWorkspaceViewModel workspace) return;
        workspace.Selection.SelectSummaryDate(date);
        eventArgs.Handled = true;
    }

    private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs eventArgs)
    {
        if (sender is not ScrollViewer viewer) return;
        viewer.ScrollToVerticalOffset(viewer.VerticalOffset - eventArgs.Delta);
        eventArgs.Handled = true;
    }

    private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child is not null && child is not T) child = VisualTreeHelper.GetParent(child);
        return child as T;
    }
}
