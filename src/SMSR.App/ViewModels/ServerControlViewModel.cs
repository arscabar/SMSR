using System.Windows.Input;
using SMSR.App.Infrastructure;
using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed class ServerControlViewModel : ViewModelBase
{
    private readonly LocalServerHost _host;
    private readonly IPlatformActions _platform;
    private string _statusMessage = "서버를 시작하세요.";

    public ServerControlViewModel(LocalServerHost host, IPlatformActions platform)
    {
        _host = host;
        _platform = platform;
        _host.StateChanged += (_, _) => UpdateState();
        StartCommand = new RelayCommand(() => _ = StartAsync(), () => !_host.IsRunning);
        StopCommand = new RelayCommand(() => _ = StopAsync(), () => _host.IsRunning);
        CopyTokenCommand = new RelayCommand(CopyToken, () => _host.IsRunning);
        UpdateState();
    }

    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand CopyTokenCommand { get; }
    public string ServerStatus => _host.IsRunning ? "SMSR 로컬 서버 실행 중" : "SMSR 로컬 서버 중지됨";
    public string ServerAddress => _host.Address;
    public string McpEndpoint => _host.IsRunning ? $"{ServerAddress}/mcp" : "-";
    public string StatusMessage { get => _statusMessage; private set => SetField(ref _statusMessage, value); }

    private async Task StartAsync()
    {
        try { await _host.StartAsync(); StatusMessage = "로컬 서버를 시작했습니다."; }
        catch (Exception exception) { StatusMessage = $"로컬 서버를 시작하지 못했습니다: {exception.Message}"; }
    }

    private async Task StopAsync()
    {
        try { await _host.StopAsync(); StatusMessage = "로컬 서버를 중지했습니다."; }
        catch (Exception exception) { StatusMessage = $"로컬 서버를 중지하지 못했습니다: {exception.Message}"; }
    }

    private void CopyToken() => StatusMessage = _platform.TryCopyToClipboard(_host.Token) ? "토큰을 클립보드에 복사했습니다." : "토큰을 복사하지 못했습니다.";

    private void UpdateState()
    {
        OnPropertyChanged(nameof(ServerStatus));
        OnPropertyChanged(nameof(ServerAddress));
        OnPropertyChanged(nameof(McpEndpoint));
        ((RelayCommand)StartCommand).NotifyCanExecuteChanged();
        ((RelayCommand)StopCommand).NotifyCanExecuteChanged();
        ((RelayCommand)CopyTokenCommand).NotifyCanExecuteChanged();
    }
}
