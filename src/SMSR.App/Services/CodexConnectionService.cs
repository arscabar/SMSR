using System.Diagnostics;
using System.IO;

namespace SMSR.App.Services;

public sealed record CodexConnectionState(bool McpRegistered, bool PluginInstalled, string Message);

public sealed class CodexConnectionService
{
    private const string Mcp = "smsr";
    private const string Plugin = "smsr-codex@personal";

    public async Task<CodexConnectionState> CheckAsync()
    {
        var mcp = await RunAsync(["mcp", "get", Mcp, "--json"]);
        var plugins = await RunAsync(["plugin", "list", "--json"]);
        var registered = mcp.ExitCode == 0;
        var installed = plugins.ExitCode == 0 && plugins.Output.Contains(Plugin, StringComparison.Ordinal);
        return new(registered, installed, registered && installed ? "Codex 연결이 등록되었습니다." : "Codex 초기 연결이 필요합니다.");
    }

    public async Task<CodexConnectionState> SetupAsync()
    {
        var root = FindPluginRoot();
        var exe = Environment.ProcessPath;
        if (root is null || string.IsNullOrWhiteSpace(exe))
            return new(false, false, "고정 설치 파일과 플러그인이 필요합니다.");

        var state = await CheckAsync();
        if (!state.McpRegistered)
        {
            var add = await RunAsync(["mcp", "add", Mcp, "--", exe, "--mcp-stdio"]);
            if (add.ExitCode != 0) return new(false, state.PluginInstalled, $"MCP 등록 실패: {add.Output}");
        }
        if (!state.PluginInstalled)
        {
            var market = await RunAsync(["plugin", "marketplace", "add", root]);
            if (market.ExitCode != 0) return new(true, false, $"플러그인 저장소 등록 실패: {market.Output}");
            var plugin = await RunAsync(["plugin", "add", Plugin]);
            if (plugin.ExitCode != 0) return new(true, false, $"플러그인 설치 실패: {plugin.Output}");
        }
        return new(true, true, "등록했습니다. Codex를 재시작하고 /hooks에서 SMSR 훅을 신뢰한 뒤 ‘확인했고 계속’을 누르세요.");
    }

    private static string? FindPluginRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, ".agents", "plugins", "marketplace.json"))) return directory.FullName;
        return null;
    }

    private static async Task<(int ExitCode, string Output)> RunAsync(IReadOnlyList<string> arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo("codex") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
            using var process = Process.Start(startInfo);
            if (process is null) return (-1, "codex CLI를 시작하지 못했습니다.");
            var output = await process.StandardOutput.ReadToEndAsync();
            output += await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return (process.ExitCode, output.Trim());
        }
        catch (Exception exception) { return (-1, exception.Message); }
    }
}
