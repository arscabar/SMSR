using System.Windows;
using SMSR.App.Infrastructure;
using SMSR.App.Mvp;
using SMSR.App.Services;
using SMSR.App.ViewModels;
using SMSR.App.Views;

namespace SMSR.App;

public partial class App : Application
{
    private LocalServerHost? _server;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Contains("--self-test"))
        {
            await MvpSelfCheck.RunAsync();
            Shutdown();
            return;
        }

        try
        {
            _server = new LocalServerHost();
            await _server.StartAsync();
            var viewModel = new MainWindowViewModel(_server, new WindowsPlatformActions());
            await viewModel.LoadAsync();
            MainWindow = new MainWindow(viewModel);
            MainWindow.Show();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "SMSR 시작 실패");
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _server?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.OnExit(e);
    }
}
