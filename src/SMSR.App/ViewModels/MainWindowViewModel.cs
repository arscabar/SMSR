using SMSR.App.Infrastructure;
using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed class MainWindowViewModel
{
    public MainWindowViewModel(LocalServerHost host, IPlatformActions platform)
    {
        Server = new ServerControlViewModel(host, platform);
        Workspace = new WorkflowWorkspaceViewModel(host, platform);
    }

    public ServerControlViewModel Server { get; }
    public WorkflowWorkspaceViewModel Workspace { get; }
    public Task LoadAsync() => Workspace.LoadAsync();
}
