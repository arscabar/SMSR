namespace SMSR.App.Services;

internal sealed record CodexConnectionState(
    bool CodexFound,
    bool McpRegistered,
    bool AutoTrackingRegistered,
    bool StartsWithWindows,
    bool IsConnected,
    string Message);

internal sealed class CodexConnectionService
{
    private readonly LocalServerHost _host;
    private readonly AppSettingsService _settings;
    private readonly WindowsStartupRegistration _startup = new();

    public CodexConnectionService(LocalServerHost host, AppSettingsService settings)
    {
        _host = host;
        _settings = settings;
    }

    public Task<CodexConnectionState> CheckAsync()
    {
        var codex = CodexDesktopLocator.Find();
        var configPath = CodexDesktopLocator.GetConfigPath();
        var registered = CodexMcpConfig.IsRegistered(configPath);
        var tracking = CodexAutoTrackingHook.IsRegistered(configPath);
        return Result(codex is not null, registered, tracking, StartsWithWindows(), Message(codex?.Version, registered, tracking));
    }

    public async Task<CodexConnectionState> SetupAsync()
    {
        var codex = CodexDesktopLocator.Find();
        var configPath = CodexDesktopLocator.GetConfigPath();
        try
        {
            if (!_host.IsRunning) await _host.StartAsync();
            if (!StartsWithWindows()) _startup.Enable();
            if (!_settings.Current.StartServerAutomatically || !_settings.Current.AutomateCodexIntegration)
                _settings.Save(_settings.Current with { StartServerAutomatically = true, AutomateCodexIntegration = true });
            if (!CodexMcpConfig.IsRegistered(configPath)) CodexMcpConfig.Register(configPath);
            if (!CodexAutoTrackingHook.IsRegistered(configPath)) CodexAutoTrackingHook.Register(configPath);
            return await Result(codex is not null, true, true, true, Message(codex?.Version, true, true));
        }
        catch (Exception exception)
        {
            return await Result(codex is not null, false, false, StartsWithWindows(), $"자동 설정에 실패했습니다: {exception.Message}");
        }
    }

    private string Message(string? version, bool registered, bool tracking)
    {
        var name = version is null ? "Codex 공유 환경" : $"Codex {version}";
        if (_host.IsCodexConnected && tracking) return $"{name} 연결 완료 · 도구 9개 · 요청형 그래프 준비됨";
        if (!registered || !tracking) return $"{name}의 연결과 그래프 추적 훅을 자동 구성합니다.";
        if (!StartsWithWindows()) return "MCP는 등록됐지만 자동 시작이 꺼져 있습니다. 한 번에 설정으로 복구하세요.";
        return _host.IsCodexAuthorized
            ? "자동 설정 완료 · Codex를 다시 열고 새 그래프 추적 훅을 한 번 신뢰하세요."
            : "자동 설정 완료 · Codex를 다시 연 뒤 OAuth와 그래프 추적 훅을 한 번 승인하세요.";
    }

    private Task<CodexConnectionState> Result(bool found, bool registered, bool tracking, bool startup, string message)
        => Task.FromResult(new CodexConnectionState(found, registered, tracking, startup, _host.IsCodexConnected, message));

    private bool StartsWithWindows()
    {
        try { return _startup.IsEnabled(); }
        catch { return false; }
    }
}
