using System.IO;

namespace SMSR.App.Services;

public sealed record CodexConnectionState(bool CodexFound, bool McpRegistered, string Message);

public sealed class CodexConnectionService
{
    public Task<CodexConnectionState> CheckAsync()
    {
        var codex = CodexDesktopLocator.Find();
        if (codex is null) return Result(false, false, "Codex 데스크톱 앱을 찾지 못했습니다. 현재 Windows 사용자에게 Codex가 설치되어 있는지 확인하세요.");

        var registered = CodexMcpConfig.IsRegistered(codex.ConfigPath);
        var message = registered
            ? $"Codex {codex.Version}에 SMSR HTTP MCP와 OAuth 인증 설정이 등록되어 있습니다. Codex의 MCP 설정에서 인증을 눌러 연결하세요."
            : $"Codex {codex.Version} 데스크톱 앱을 찾았습니다. 초기 연결을 눌러 MCP 설정을 등록하세요.";
        return Result(true, registered, message);
    }

    public Task<CodexConnectionState> SetupAsync()
    {
        var codex = CodexDesktopLocator.Find();
        if (codex is null) return Result(false, false, "Codex 데스크톱 앱을 찾지 못했습니다. 현재 Windows 사용자에게 Codex를 설치한 뒤 다시 시도하세요.");

        try
        {
            var backup = CodexMcpConfig.Register(codex.ConfigPath);
            var backupMessage = backup is null ? string.Empty : $" 기존 설정은 {backup}에 백업했습니다.";
            return Result(true, true, $"Codex {codex.Version}에 SMSR HTTP MCP를 OAuth 방식으로 등록했습니다.{backupMessage} Codex를 완전히 재시작한 뒤 MCP 설정에서 인증을 누르고 SMSR 승인 화면에서 연결을 승인하세요.");
        }
        catch (Exception exception)
        {
            return Result(true, false, $"Codex MCP 설정을 저장하지 못했습니다: {exception.Message}");
        }
    }

    private static Task<CodexConnectionState> Result(bool found, bool registered, string message)
        => Task.FromResult(new CodexConnectionState(found, registered, message));
}
