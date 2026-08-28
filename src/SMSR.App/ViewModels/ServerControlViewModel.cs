using System.Windows.Input;
using SMSR.App.Infrastructure;
using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed class ServerControlViewModel : ViewModelBase
{
    private readonly LocalServerHost _host;
    private readonly IPlatformActions _platform;
    private readonly CodexConnectionService _codex = new();
    private string _statusMessage = "서버 상태를 확인 중입니다.";
    private string _codexStatus = "Codex 연결을 확인 중입니다.";

    public ServerControlViewModel(LocalServerHost host, IPlatformActions platform)
    {
        _host = host;
        _platform = platform;
        _host.StateChanged += (_, _) => UpdateState();
        StartCommand = new RelayCommand(() => _ = StartAsync(), () => !_host.IsRunning);
        StopCommand = new RelayCommand(() => _ = StopAsync(), () => _host.IsRunning);
        CopyTokenCommand = new RelayCommand(CopyToken, () => _host.IsRunning);
        SetupConnectionCommand = new RelayCommand(() => _ = SetupConnectionAsync());
        ConfirmConnectionCommand = new RelayCommand(() => _ = ConfirmConnectionAsync());
        UpdateState();
        _ = ConfirmConnectionAsync();
    }

    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand CopyTokenCommand { get; }
    public ICommand SetupConnectionCommand { get; }
    public ICommand ConfirmConnectionCommand { get; }
    public string ServerStatus => _host.IsRunning ? "● 서버 실행 중" : "● 서버 중지됨";
    public string ServerAddress => _host.Address;
    public string McpEndpoint => _host.IsRunning ? $"{ServerAddress}/mcp" : "-";
    public string StatusMessage { get => _statusMessage; private set => SetField(ref _statusMessage, value); }
    public string CodexStatus { get => _codexStatus; private set => SetField(ref _codexStatus, value); }

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

    private async Task SetupConnectionAsync()
    {
        CodexStatus = "Codex 연결을 등록하는 중입니다.";
        CodexStatus = (await _codex.SetupAsync()).Message;
    }

    private async Task ConfirmConnectionAsync()
    {
        CodexStatus = (await _codex.CheckAsync()).Message;
    }

    private void UpdateState()
    {
        StatusMessage = _host.IsRunning ? "MCP 연결을 받을 준비가 되었습니다." : "서버가 중지되었습니다. 시작 버튼으로 다시 실행할 수 있습니다.";
        OnPropertyChanged(nameof(ServerStatus));
        OnPropertyChanged(nameof(ServerAddress));
        OnPropertyChanged(nameof(McpEndpoint));
        ((RelayCommand)StartCommand).NotifyCanExecuteChanged();
        ((RelayCommand)StopCommand).NotifyCanExecuteChanged();
        ((RelayCommand)CopyTokenCommand).NotifyCanExecuteChanged();
    }
}
