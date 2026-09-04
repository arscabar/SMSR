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
        var executable = ResolveBridge();
        var registered = executable is not null && CodexMcpConfig.IsRegistered(configPath, executable);
        var tracking = executable is not null && CodexAutoTrackingHook.IsRegistered(configPath, executable);
        return Result(codex is not null, registered, tracking, StartsWithWindows(), Message(codex?.Version, registered, tracking));
    }

    public async Task<CodexConnectionState> SetupAsync()
    {
        var codex = CodexDesktopLocator.Find();
        var configPath = CodexDesktopLocator.GetConfigPath();
        try
        {
            var executable = ResolveBridge()
                ?? throw new InvalidOperationException("SMSR 실행 파일 경로를 확인할 수 없습니다.");
            if (!_host.IsRunning) await _host.StartAsync();
            if (!StartsWithWindows()) _startup.Enable();
            if (!_settings.Current.StartServerAutomatically || !_settings.Current.AutomateCodexIntegration)
                _settings.Save(_settings.Current with { StartServerAutomatically = true, AutomateCodexIntegration = true });
            if (!CodexMcpConfig.IsRegistered(configPath, executable)) CodexMcpConfig.Register(configPath, executable);
            if (!CodexAutoTrackingHook.IsRegistered(configPath, executable))
                CodexAutoTrackingHook.Register(configPath, executable);
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
        if (_host.IsCodexConnected && tracking) return $"{name} 연결 완료 · 도구 12개 · 일일 기록·그래프·AI 요약 준비됨";
        if (!registered || !tracking) return $"{name}의 연결과 그래프 추적 훅을 자동 구성합니다.";
        if (!StartsWithWindows()) return "MCP는 등록됐지만 자동 시작이 꺼져 있습니다. 한 번에 설정으로 복구하세요.";
        return "자동 로컬 브리지 설정 완료 · 인증창 없이 연결됩니다. Codex를 한 번 다시 연 뒤 첫 SMSR 요청을 기다리는 중입니다.";
    }

    private Task<CodexConnectionState> Result(bool found, bool registered, bool tracking, bool startup, string message)
        => Task.FromResult(new CodexConnectionState(found, registered, tracking, startup, _host.IsCodexConnected, message));

    private bool StartsWithWindows()
    {
        try { return _startup.IsEnabled(); }
        catch { return false; }
    }

    private static string? ResolveBridge()
        => Environment.ProcessPath is { } path ? CodexBridgeExecutable.Ensure(path) : null;
}
