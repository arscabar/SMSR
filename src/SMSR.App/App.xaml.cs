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
    private PetController? _pet;
    private MainInstanceGuard? _mainInstance;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        if (e.Args.Contains("--smsr-auto-track-hook"))
        {
            try { await CodexHookRunner.RunAsync(); Shutdown(); }
            catch (Exception exception) { CodexHookRunner.LogFailure("startup", exception); Shutdown(-1); }
            return;
        }
        if (e.Args.Contains("--smsr-git-post-commit"))
        {
            try { await GitAutoIndexRunner.RunAsync(); Shutdown(); }
            catch { Shutdown(-1); }
            return;
        }
        if (e.Args.Contains("--mcp-stdio"))
        {
            try
            {
                var isolated = e.Args.Contains("--isolated-test", StringComparer.OrdinalIgnoreCase);
                await StdioMcpHost.RunAsync(!isolated);
                Shutdown();
            }
            catch (Exception exception)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smsr-stdio-error.txt"), exception.ToString());
                Shutdown(-1);
            }
            return;
        }
        if (e.Args.Contains("--ensure-bridge"))
        {
            try
            {
                _ = CodexBridgeExecutable.Ensure(Environment.ProcessPath
                    ?? throw new InvalidOperationException("SMSR 실행 파일 경로를 확인할 수 없습니다."));
                Shutdown();
            }
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
            catch (Exception exception)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smsr-codex-config-self-test-error.txt"), exception.ToString());
                Shutdown(-1);
            }
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
        if (e.Args.Contains("--graph-self-test"))
        {
            try { await GraphIndexSelfCheck.RunAsync(); Shutdown(); }
            catch (Exception exception)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smsr-graph-self-test-error.txt"), exception.ToString());
                Shutdown(-1);
            }
            return;
        }
        if (e.Args.Contains("--graph-code-project-self-test"))
        {
            try
            {
                await GraphCodeProjectSelfCheck.RunAsync(e.Args[Array.IndexOf(e.Args, "--graph-code-project-self-test") + 1]);
                Shutdown();
            }
            catch (Exception exception)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                    "smsr-graph-code-project-self-test-error.txt"), exception.ToString());
                Shutdown(-1);
            }
            return;
        }
        if (e.Args.Contains("--graph-static-self-test"))
        {
            try { await GraphStaticSelfCheck.RunAsync(); await GraphCodeIndexSelfCheck.RunAsync(); Shutdown(); }
            catch (Exception exception)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                    "smsr-graph-static-self-test-error.txt"), exception.ToString());
                Shutdown(-1);
            }
            return;
        }
        if (e.Args.Contains("--graph-lsp-self-test"))
        {
            try { await GraphLspSelfCheck.RunAsync(); Shutdown(); }
            catch (Exception exception)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smsr-graph-lsp-self-test-error.txt"), exception.ToString());
                Shutdown(-1);
            }
            return;
        }
        if (e.Args.Contains("--graph-jdt-product-self-test"))
        {
            try { await GraphJdtProductSelfCheck.RunAsync(); Shutdown(); }
            catch(Exception exception)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"smsr-graph-jdt-product-self-test-error.txt"),exception.ToString());
                Shutdown(-1);
            }
            return;
        }
        if (e.Args.Contains("--graph-jdt-self-test"))
        {
            try { await GraphJdtSelfCheck.RunAsync(e.Args[Array.IndexOf(e.Args,"--graph-jdt-self-test")+1]); Shutdown(); }
            catch(Exception exception)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"smsr-graph-jdt-self-test-error.txt"),exception.ToString());
                Shutdown(-1);
            }
            return;
        }
        if (e.Args.Contains("--dashboard-panel-preview-self-test"))
        {
            try
            {
                await DashboardPanelPreviewSelfCheck.RunAsync(e.Args[Array.IndexOf(e.Args, "--dashboard-panel-preview-self-test") + 1]);
                Shutdown();
            }
            catch (Exception exception)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                    "smsr-dashboard-panel-preview-self-test-error.txt"), exception.ToString());
                Shutdown(-1);
            }
            return;
        }
        if (e.Args.Contains("--graph-document-self-test"))
        {
            try { await GraphDocumentSelfCheck.RunAsync(); Shutdown(); }
            catch (Exception exception)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                    "smsr-graph-document-self-test-error.txt"), exception.ToString());
                Shutdown(-1);
            }
            return;
        }
        if (e.Args.Contains("--graph-role-self-test"))
        {
            try { await GraphRoleAcceptance.RunAsync(); Shutdown(); }
            catch (Exception exception)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                    "smsr-graph-role-self-test-error.txt"), exception.ToString());
                Shutdown(-1);
            }
            return;
        }
        if (e.Args.Contains("--graph-role-live-self-test"))
        {
            try { await GraphRoleLiveSelfCheck.RunAsync(); Shutdown(); }
            catch (Exception exception)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                    "smsr-graph-role-live-self-test-error.txt"), exception.ToString()); Shutdown(-1);
            }
            return;
        }
        if (e.Args.Contains("--graph-overview-large-self-test"))
        {
            try { await GraphOverviewLargeSelfCheck.RunAsync(); Shutdown(); }
            catch (Exception exception)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"smsr-graph-overview-large-self-test-error.txt"),exception.ToString());
                Shutdown(-1);
            }
            return;
        }
        if(e.Args.Contains("--graph-quality-preview"))
        {
            try{await GraphQualityPreview.RunAsync(e.Args[Array.IndexOf(e.Args,"--graph-quality-preview")+1]);Shutdown();}
            catch(Exception exception){System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"smsr-graph-quality-preview-error.txt"),exception.ToString());Shutdown(-1);}
            return;
        }
        if(e.Args.Contains("--graph-feature-preview"))
        {
            try{await GraphFeaturePreview.RunAsync(e.Args[Array.IndexOf(e.Args,"--graph-feature-preview")+1]);Shutdown();}
            catch(Exception exception){System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"smsr-graph-feature-preview-error.txt"),exception.ToString());Shutdown(-1);}
            return;
        }
        if(e.Args.Contains("--graph-knowledge-project-self-test"))
        {
            try{await GraphKnowledgeProjectSelfCheck.RunAsync(e.Args[Array.IndexOf(e.Args,"--graph-knowledge-project-self-test")+1]);Shutdown();}
            catch(Exception exception){System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"smsr-graph-knowledge-project-self-test-error.txt"),exception.ToString());Shutdown(-1);}
            return;
        }
        if (e.Args.Contains("--graph-knowledge-self-test"))
        {
            try { await GraphKnowledgeSelfCheck.RunAsync(); Shutdown(); }
            catch (Exception exception)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                    "smsr-graph-knowledge-self-test-error.txt"), exception.ToString());
                Shutdown(-1);
            }
            return;
        }
        if (e.Args.Contains("--graph-deep-self-test"))
        {
            try
            {
                await GraphDeepStorageSelfCheck.RunAsync();
                await GraphDeepLanguageSelfCheck.RunAsync();
                Shutdown();
            }
            catch (Exception exception)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                    "smsr-graph-deep-self-test-error.txt"), exception.ToString());
                Shutdown(-1);
            }
            return;
        }
        if (e.Args.Contains("--graph-advanced-self-test"))
        {
            try { await GraphAdvancedSelfCheck.RunAsync(); Shutdown(); }
            catch (Exception exception)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smsr-graph-advanced-self-test-error.txt"), exception.ToString());
                Shutdown(-1);
            }
            return;
        }
        if (e.Args.Contains("--graph-responsiveness-self-test"))
        {
            try { await GraphResponsivenessSelfCheck.RunAsync(); Shutdown(); }
            catch (Exception exception)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smsr-graph-responsiveness-error.txt"), exception.ToString());
                Shutdown(-1);
            }
            return;
        }
        if (e.Args.Contains("--graph-benchmark"))
        {
            try { await GraphPerformanceSelfCheck.RunAsync(); Shutdown(); }
            catch (Exception exception)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smsr-graph-benchmark-error.txt"), exception.ToString());
                Shutdown(-1);
            }
            return;
        }
        if (e.Args.Contains("--update-self-test"))
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"smsr-update-{Guid.NewGuid():N}");
            try { await AppUpdateSelfCheck.RunPublishedReleaseAsync(path); Shutdown(); }
            catch (Exception exception)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smsr-update-self-test-error.txt"), exception.ToString());
                Shutdown(-1);
            }
            finally { if (System.IO.Directory.Exists(path)) System.IO.Directory.Delete(path, true); }
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
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        var startInBackground = e.Args.Contains("--background", StringComparer.OrdinalIgnoreCase);
        _mainInstance = MainInstanceGuard.TryAcquire();
        if (_mainInstance is null)
        {
            if (!startInBackground) MainInstanceGuard.RequestActivation();
            Shutdown();
            return;
        }
        try
        {
            var ensureServer = e.Args.Contains("--ensure-server", StringComparer.OrdinalIgnoreCase);
            var settings = new AppSettingsService();
            AppThemeService.Apply(settings.Current.DashboardTheme);
            settings.Changed += (_, _) => Dispatcher.Invoke(() => AppThemeService.Apply(settings.Current.DashboardTheme));
            _server = new LocalServerHost(dashboardTheme: () => settings.Current.DashboardTheme);
            if (ensureServer || settings.Current.StartServerAutomatically) await _server.StartAsync();
            if (settings.Current.AutomateCodexIntegration && !e.Args.Contains("--skip-codex-integration", StringComparer.OrdinalIgnoreCase))
                await new CodexConnectionService(_server, settings).SetupAsync();
            var viewModel = new MainWindowViewModel(_server, new WindowsPlatformActions(), settings, ExitApplication);
            await viewModel.LoadAsync();
            MainWindow = new MainWindow(viewModel, () => settings.Current.MinimizeToTray);
            var window = (MainWindow)MainWindow;
            _pet = new PetController(settings, viewModel.Workspace, () => window.ShowFromTray());
            void Execute(System.Windows.Input.ICommand command)
            {
                if (command.CanExecute(null)) command.Execute(null);
            }
            _tray = new TrayStatusIcon(
                () => Dispatcher.Invoke(() => window.ShowFromTray()),
                () => Dispatcher.Invoke(() => Execute(viewModel.Workspace.OpenDashboardCommand)),
                () => Dispatcher.Invoke(() => Execute(viewModel.Server.StartCommand)),
                () => Dispatcher.Invoke(() => Execute(viewModel.Server.StopCommand)),
                () => Dispatcher.Invoke(() => window.ShowFromTray(2)),
                () => Dispatcher.Invoke(() => _pet.Toggle()),
                ExitApplication,
                () => new(_server.IsRunning, viewModel.Server.IsCodexConnected,
                    viewModel.Workspace.OpenDashboardCommand.CanExecute(null)),
                () => (_pet.CanShow, _pet.IsVisible));
            _server.StateChanged += OnServerStateChanged;
            _mainInstance.Listen(() => Dispatcher.BeginInvoke(() => window.ShowFromTray()));
            if (!startInBackground) MainWindow.Show();
            if (!e.Args.Contains("--skip-update-check", StringComparer.OrdinalIgnoreCase))
                _ = viewModel.Settings.CheckForUpdatesOnStartupAsync();
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
        _pet?.Dispose();
        _server?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _mainInstance?.Dispose();
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
