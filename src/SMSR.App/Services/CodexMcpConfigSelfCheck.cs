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
                || !text.Contains("enabled = true", StringComparison.Ordinal)
                || text.Contains("--mcp-stdio", StringComparison.Ordinal)
                || backup is null || !File.Exists(backup))
                throw new InvalidOperationException("Codex MCP 설정 등록·백업 검증이 실패했습니다.");

            CodexMcpConfig.Register(path);
            if (File.ReadAllText(path).Split("[mcp_servers.smsr]", StringSplitOptions.None).Length != 2)
                throw new InvalidOperationException("Codex MCP 설정 중복 방지가 실패했습니다.");
            var hooksPath = Path.Combine(directory, "hooks.json");
            File.WriteAllText(hooksPath, "{\"hooks\":{\"Stop\":[{\"hooks\":[{\"type\":\"command\",\"command\":\"other.exe\"}]}]}}");
            var fakeExecutable = Path.Combine(directory, "SMSR App.exe");
            var hooksBackup = CodexAutoTrackingHook.Register(path, fakeExecutable);
            var hooksText = File.ReadAllText(hooksPath);
            if (!CodexAutoTrackingHook.IsRegistered(path, fakeExecutable)
                || !hooksText.Contains("other.exe", StringComparison.Ordinal)
                || hooksText.Split("SMSR automatic tracking", StringSplitOptions.None).Length != 2
                || hooksText.Contains("record_lifecycle", StringComparison.Ordinal)
                || hooksBackup is null || !File.Exists(hooksBackup))
                throw new InvalidOperationException("Codex 요청형 그래프 훅 병합 검증이 실패했습니다.");
            if (CodexAutoTrackingHook.Register(path, fakeExecutable) is not null)
                throw new InvalidOperationException("Codex 자동 추적 훅 중복 방지가 실패했습니다.");
            var planningSettings = new AppSettings(PlanningPrompt: "PLAN {projectId} {taskId}");
            var context = CodexAutoTrackingContext.CreateOutput("{\"session_id\":\"session-1\",\"cwd\":\"C:\\\\work\\\\SMSR\",\"prompt\":\"SECRET\"}", planningSettings);
            if (!context.Contains("session-1", StringComparison.Ordinal) || !context.Contains("SMSR", StringComparison.Ordinal)
                || !context.Contains("graph tracking is opt-in", StringComparison.Ordinal)
                || !context.Contains("Ignore source_thread_id", StringComparison.Ordinal)
                || !context.Contains("current Codex task/session ID", StringComparison.Ordinal)
                || !context.Contains("omit workflowId in the first save_plan", StringComparison.Ordinal)
                || !context.Contains("projectName__yyyyMMdd-HHmmssfff", StringComparison.Ordinal)
                || !context.Contains("SUCCESS, FAILED, or BLOCKED", StringComparison.Ordinal)
                || !context.Contains("PLAN SMSR session-1", StringComparison.Ordinal)
                || context.Contains("SECRET", StringComparison.Ordinal))
                throw new InvalidOperationException("Codex 자동 추적 컨텍스트 검증이 실패했습니다.");
            var noPlanning = CodexAutoTrackingContext.CreateOutput("{\"session_id\":\"session-1\",\"cwd\":\"C:\\\\work\\\\SMSR\"}", planningSettings with { RequirePlanReview = false });
            if (noPlanning.Contains("User-configured SMSR planning policy", StringComparison.Ordinal))
                throw new InvalidOperationException("작업계획 검토 비활성화 검증이 실패했습니다.");
            if (PlanningPromptSettings.Normalize(PlanningPromptSettings.LegacyDefault) != PlanningPromptSettings.Default)
                throw new InvalidOperationException("이전 작업계획 기본 문구 마이그레이션이 실패했습니다.");
            if (PlanningPromptSettings.Normalize(PlanningPromptSettings.PreviousDefault) != PlanningPromptSettings.Default)
                throw new InvalidOperationException("직전 작업계획 기본 문구 마이그레이션이 실패했습니다.");
            if (WindowsStartupRegistration.BuildCommand(@"C:\Program Files\SMSR\SMSR.App.exe")
                != "\"C:\\Program Files\\SMSR\\SMSR.App.exe\" --background")
                throw new InvalidOperationException("Windows 자동 시작 명령 검증이 실패했습니다.");

            CodexAutoTrackingHook.Unregister(path);
            var cleanedHooks = File.ReadAllText(hooksPath);
            if (cleanedHooks.Contains("SMSR automatic tracking", StringComparison.Ordinal)
                || !cleanedHooks.Contains("other.exe", StringComparison.Ordinal))
                throw new InvalidOperationException("Codex 자동 추적 훅 제거 검증이 실패했습니다.");
            CodexMcpConfig.Unregister(path);
            var cleanedConfig = File.ReadAllText(path);
            if (cleanedConfig.Contains("[mcp_servers.smsr]", StringComparison.Ordinal)
                || !cleanedConfig.Contains("[mcp_servers.other]", StringComparison.Ordinal))
                throw new InvalidOperationException("Codex MCP 설정 제거 검증이 실패했습니다.");
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
