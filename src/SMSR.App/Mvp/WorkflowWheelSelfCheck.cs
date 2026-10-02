using System.Windows;
using WpfComboBox = System.Windows.Controls.ComboBox;
using System.Windows.Input;
using System.Windows.Media;
using SMSR.App.Views;

namespace SMSR.App.Mvp;

internal static class WorkflowWheelSelfCheck
{
    internal static void Run()
    {
        var panel = new WorkflowPanel();
        panel.Measure(new System.Windows.Size(500, 600)); panel.Arrange(new Rect(0, 0, 500, 600));
        var combos = Children(panel).OfType<WpfComboBox>().Take(2).ToArray();
        if (combos.Length != 2) throw new Exception("Workflow dropdown test controls missing");
        foreach (var combo in combos)
        {
            var wheel = new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, -120)
                { RoutedEvent = Mouse.PreviewMouseWheelEvent };
            combo.RaiseEvent(wheel);
            if (wheel.Handled) throw new Exception("Outer workflow panel captured a dropdown preview wheel");
        }
    }
    private static IEnumerable<DependencyObject> Children(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i); yield return child;
            foreach (var nested in Children(child)) yield return nested;
        }
    }
}
