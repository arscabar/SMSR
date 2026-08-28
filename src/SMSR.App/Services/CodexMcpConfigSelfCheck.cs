using System.IO;

namespace SMSR.App.Services;

internal static class CodexMcpConfigSelfCheck
{
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"smsr-codex-config-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "config.toml");
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, "[mcp_servers.other]\ncommand = \"other.exe\"\nargs = []\n");
            var backup = CodexMcpConfig.Register(path);
            var text = File.ReadAllText(path);
            if (!CodexMcpConfig.IsRegistered(path)
                || !text.Contains("[mcp_servers.other]", StringComparison.Ordinal)
                || !text.Contains("auth = \"oauth\"", StringComparison.Ordinal)
                || text.Contains("--mcp-stdio", StringComparison.Ordinal)
                || backup is null || !File.Exists(backup))
                throw new InvalidOperationException("Codex MCP 설정 등록·백업 검증이 실패했습니다.");

            CodexMcpConfig.Register(path);
            if (File.ReadAllText(path).Split("[mcp_servers.smsr]", StringSplitOptions.None).Length != 2)
                throw new InvalidOperationException("Codex MCP 설정 중복 방지가 실패했습니다.");
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
