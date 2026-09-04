using System.Windows.Input;
using SMSR.App.Infrastructure;
using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed partial class ServerControlViewModel
{
    private readonly CodexConnectionService _codex;
    private string _codexStatus = "Codex 연결을 확인 중입니다.";
    private bool _isSettingUp;

    public ICommand SetupConnectionCommand { get; }
    public string CodexStatus { get => _codexStatus; private set => SetField(ref _codexStatus, value); }
    public bool IsCodexConnected => _host.IsCodexConnected;
    public bool NeedsCodexSetup => !IsCodexConnected;
    public string CodexConnectionTitle => IsCodexConnected ? "● Codex 연결됨 · 도구 12개 · 일일 기록·그래프·AI 요약" : "Codex 연결 설정";
    public bool IsSettingUp { get => _isSettingUp; private set => SetField(ref _isSettingUp, value); }

    private async Task SetupConnectionAsync()
    {
        IsSettingUp = true;
        ((RelayCommand)SetupConnectionCommand).NotifyCanExecuteChanged();
        CodexStatus = "서버·Windows 자동 시작·Codex MCP 설정을 구성하는 중입니다.";
        try { CodexStatus = (await _codex.SetupAsync()).Message; }
        finally
        {
            IsSettingUp = false;
            ((RelayCommand)SetupConnectionCommand).NotifyCanExecuteChanged();
            UpdateState();
        }
    }

    private async Task ConfirmConnectionAsync()
    {
        CodexStatus = IsCodexConnected
            ? "실제 MCP 연결이 확인되었습니다. SMSR 도구 12개를 사용할 수 있습니다."
            : (await _codex.CheckAsync()).Message;
    }
}
