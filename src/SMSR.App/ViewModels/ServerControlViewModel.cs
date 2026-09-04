using System.Windows.Input;
using SMSR.App.Infrastructure;
using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed partial class ServerControlViewModel : ViewModelBase
{
    private readonly LocalServerHost _host;
    private string _statusMessage = "서버 상태를 확인 중입니다.";

    public ServerControlViewModel(LocalServerHost host, AppSettingsService settings)
    {
        _host = host;
        _codex = new(host, settings);
        _host.StateChanged += OnHostStateChanged;
        _host.AuthorizationChanged += OnHostStateChanged;
        StartCommand = new RelayCommand(() => _ = StartAsync(), () => !_host.IsRunning);
        StopCommand = new RelayCommand(() => _ = StopAsync(), () => _host.IsRunning);
        SetupConnectionCommand = new RelayCommand(() => _ = SetupConnectionAsync(), () => !IsSettingUp);
        UpdateState();
        _ = ConfirmConnectionAsync();
    }

    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public string ServerStatus => _host.IsRunning ? "● 서버 실행 중" : "● 서버 중지됨";
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

    private void UpdateState()
    {
        StatusMessage = _host.IsRunning ? "MCP 연결을 받을 준비가 되었습니다." : "서버가 중지되었습니다. 시작 버튼으로 다시 실행할 수 있습니다.";
        OnPropertyChanged(nameof(ServerStatus));
        OnPropertyChanged(nameof(ServerAddress));
        OnPropertyChanged(nameof(McpEndpoint));
        OnPropertyChanged(nameof(IsCodexConnected));
        OnPropertyChanged(nameof(NeedsCodexSetup));
        OnPropertyChanged(nameof(CodexConnectionTitle));
        if (IsCodexConnected) CodexStatus = "실제 MCP 연결이 확인되었습니다. SMSR 도구 10개를 사용할 수 있습니다.";
        ((RelayCommand)StartCommand).NotifyCanExecuteChanged();
        ((RelayCommand)StopCommand).NotifyCanExecuteChanged();
    }

    private void OnHostStateChanged(object? sender, EventArgs eventArgs)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) UpdateState();
        else _ = dispatcher.BeginInvoke(UpdateState);
    }
}
