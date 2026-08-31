using System.Windows;
using System.Windows.Input;
using SMSR.App.ViewModels;

namespace SMSR.App.Views;

public partial class MainWindow : Window
{
    private bool _allowClose;
    private readonly Func<bool> _minimizeToTray;
    public MainWindow(MainWindowViewModel viewModel, Func<bool> minimizeToTray)
    {
        InitializeComponent();
        DataContext = viewModel;
        _minimizeToTray = minimizeToTray;
    }

    public void ShowFromTray(int? tabIndex = null)
    {
        Show();
        WindowState = WindowState.Normal;
        if (tabIndex is >= 0 and < 4) MainTabs.SelectedIndex = tabIndex.Value;
        Activate();
    }

    public void AllowClose() => _allowClose = true;

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_allowClose && _minimizeToTray())
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }
}
