using SMSR.App.Infrastructure;
using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed class MainWindowViewModel
{
    public MainWindowViewModel(LocalServerHost host, IPlatformActions platform, AppSettingsService settings,
        Action? exitApplication = null)
    {
        Server = new ServerControlViewModel(host);
        Workspace = new WorkflowWorkspaceViewModel(host, platform);
        Settings = new SettingsViewModel(settings, host, platform);
        ExitCommand = new RelayCommand(exitApplication ?? (() => { }));
    }

    public ServerControlViewModel Server { get; }
    public WorkflowWorkspaceViewModel Workspace { get; }
    public SettingsViewModel Settings { get; }
    public RelayCommand ExitCommand { get; }
    public Task LoadAsync() => Workspace.LoadAsync();
}
