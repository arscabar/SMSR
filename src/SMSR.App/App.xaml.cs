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
        if (e.Args.Contains("--smsr-auto-track-hook"))
        {
            try { await CodexHookRunner.RunAsync(); Shutdown(); }
            catch { Shutdown(-1); }
            return;
        }
        if (e.Args.Contains("--uninstall-cleanup"))
        {
            try { CodexIntegrationCleanup.Run(); Shutdown(); }
            catch { Shutdown(-1); }
            return;
        }
        if (e.Args.Contains("--codex-config-self-test"))
        {
            try
            {
                if (!System.IO.Path.IsPathFullyQualified(CodexDesktopLocator.GetConfigPath()))
                    throw new InvalidOperationException("Codex 공유 설정 경로 확인 실패");
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
            var startInBackground = e.Args.Contains("--background", StringComparer.OrdinalIgnoreCase);
            var settings = new AppSettingsService();
            AppThemeService.Apply(settings.Current.DashboardTheme);
            settings.Changed += (_, _) => Dispatcher.Invoke(() => AppThemeService.Apply(settings.Current.DashboardTheme));
            _server = new LocalServerHost(dashboardTheme: () => settings.Current.DashboardTheme);
            if (settings.Current.StartServerAutomatically) await _server.StartAsync();
            if (settings.Current.AutomateCodexIntegration)
                await new CodexConnectionService(_server, settings).SetupAsync();
            var viewModel = new MainWindowViewModel(_server, new WindowsPlatformActions(), settings, ExitApplication);
            await viewModel.LoadAsync();
            MainWindow = new MainWindow(viewModel, () => settings.Current.MinimizeToTray);
            var window = (MainWindow)MainWindow;
            void Execute(System.Windows.Input.ICommand command)
            {
                if (command.CanExecute(null)) command.Execute(null);
            }
            _tray = new TrayStatusIcon(
                () => Dispatcher.Invoke(() => window.ShowFromTray()),
                () => Dispatcher.Invoke(() => Execute(viewModel.Workspace.OpenDashboardCommand)),
                () => Dispatcher.Invoke(() => Execute(viewModel.Server.StartCommand)),
                () => Dispatcher.Invoke(() => Execute(viewModel.Server.StopCommand)),
                () => Dispatcher.Invoke(() => window.ShowFromTray(3)),
                ExitApplication,
                () => new(_server.IsRunning, viewModel.Server.IsCodexConnected,
                    viewModel.Workspace.OpenDashboardCommand.CanExecute(null)));
            _server.StateChanged += OnServerStateChanged;
            if (!startInBackground) MainWindow.Show();
        }
        catch (Exception exception)
        {
            System.Windows.MessageBox.Show(exception.Message, "SMSR 시작 실패");
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_server is not null) _server.StateChanged -= OnServerStateChanged;
        _tray?.Dispose();
        _server?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.OnExit(e);
    }

    private void ExitApplication()
    {
        ((MainWindow?)MainWindow)?.AllowClose();
        Shutdown();
    }

    private void OnServerStateChanged(object? sender, EventArgs eventArgs)
        => _ = Dispatcher.BeginInvoke(() => _tray?.RefreshStatus());
}
