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
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        if (e.Args.Contains("--codex-config-self-test"))
        {
            try
            {
                if (CodexDesktopLocator.Find() is null) throw new InvalidOperationException("Codex 데스크톱 앱 탐지 실패");
                CodexMcpConfigSelfCheck.Run();
                Shutdown();
            }
            catch { Shutdown(-1); }
            return;
        }
        if (e.Args.Contains("--oauth-self-test"))
        {
            try { await OAuthStandaloneSelfCheck.RunAsync(); Shutdown(); }
            catch (Exception exception)
            {
                var errorPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smsr-oauth-self-test-error.txt");
                System.IO.File.WriteAllText(errorPath, exception.ToString());
                Shutdown(-1);
            }
            return;
        }
        if (e.Args.Contains("--tracking-self-test"))
        {
            try { await TrackingContractSelfCheck.RunAsync(); Shutdown(); }
            catch (Exception exception)
            {
                var errorPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smsr-tracking-self-test-error.txt");
                System.IO.File.WriteAllText(errorPath, exception.ToString());
                Shutdown(-1);
            }
            return;
        }
        if (e.Args.Contains("--self-test"))
        {
            try { await MvpSelfCheck.RunAsync(); }
            catch (Exception exception)
            {
                var errorPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smsr-self-test-error.txt");
                System.IO.File.WriteAllText(errorPath, exception.ToString());
                Shutdown(-1);
                return;
            }
            Shutdown();
            return;
        }
        try
        {
            var settings = new AppSettingsService();
            AppThemeService.Apply(settings.Current.DashboardTheme);
            settings.Changed += (_, _) => Dispatcher.Invoke(() => AppThemeService.Apply(settings.Current.DashboardTheme));
            _server = new LocalServerHost(dashboardTheme: () => settings.Current.DashboardTheme);
            if (settings.Current.StartServerAutomatically) await _server.StartAsync();
            var viewModel = new MainWindowViewModel(_server, new WindowsPlatformActions(), settings, ExitApplication);
            await viewModel.LoadAsync();
            MainWindow = new MainWindow(viewModel, () => settings.Current.MinimizeToTray);
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
