using System.Windows;
using SMSR.App.Infrastructure;
using SMSR.App.Mvp;
using SMSR.App.Services;
using SMSR.App.ViewModels;
using SMSR.App.Views;
using WpfApplication = System.Windows.Application;

namespace SMSR.App;

public partial class App : WpfApplication
{
    private LocalServerHost? _server;
    private TrayStatusIcon? _tray;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        if (e.Args.Contains("--self-test"))
        {
            await MvpSelfCheck.RunAsync();
            Shutdown();
            return;
        }
        if (e.Args.Contains("--mcp-stdio"))
        {
            await StdioMcpHost.RunAsync();
            Shutdown();
            return;
        }

        try
        {
            _server = new LocalServerHost();
            await _server.StartAsync();
            var viewModel = new MainWindowViewModel(_server, new WindowsPlatformActions(), ExitApplication);
            await viewModel.LoadAsync();
            MainWindow = new MainWindow(viewModel);
            _tray = new TrayStatusIcon(((MainWindow)MainWindow).ShowFromTray, ExitApplication);
            MainWindow.Show();
        }
        catch (Exception exception)
        {
            System.Windows.MessageBox.Show(exception.Message, "SMSR 시작 실패");
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _server?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.OnExit(e);
    }

    private void ExitApplication()
    {
        ((MainWindow?)MainWindow)?.AllowClose();
        Shutdown();
    }
}
