using System.Diagnostics;
using System.IO;
using System.Text.Json;
using SMSR.App.Services;

namespace SMSR.App.Mvp;

internal static class GraphRoleRunner
{
    internal static async Task<GraphRoleRequest> RunAsync(string projectId, GraphRoleContext context, CancellationToken ct)
    {
        var executable = CodexRoleExecutable.Find() ?? throw new InvalidOperationException("Codex 실행기를 찾지 못했습니다.");
        var folder = Path.Combine(Path.GetTempPath(), "smsr-role-run-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var schema = Path.Combine(folder, "schema.json"); var output = Path.Combine(folder, "result.json");
            await File.WriteAllTextAsync(schema, GraphRoleOutputSchema.Create(context), ct);
            var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var argument in new[] { "exec", "--ignore-user-config", "--ephemeral", "--json",
                "--skip-git-repo-check", "--sandbox", "read-only", "-C", folder, "--output-schema", schema,
                "-o", output, "-c", "features.hooks=false", "-c", "features.apps=false", "-c",
                "features.shell_tool=false", "-c", "features.unified_exec=false", "-c", "project_doc_max_bytes=0", "-" })
                start.ArgumentList.Add(argument);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Codex 실행 실패");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct); deadline.CancelAfter(TimeSpan.FromMinutes(5));
            using var cancel = deadline.Token.Register(() => { try { process.Kill(true); } catch (InvalidOperationException) { } });
            var events = GraphProcessOutput.ReadAsync(process.StandardOutput.BaseStream, deadline.Token, 256 * 1024);
            var errors = GraphProcessOutput.ReadAsync(process.StandardError.BaseStream, deadline.Token, 64 * 1024);
            await process.StandardInput.BaseStream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(GraphRolePrompt.Create(context)), deadline.Token);
            process.StandardInput.Close();
            await process.WaitForExitAsync(deadline.Token); var eventText = await events; var errorText = await errors;
            if (process.ExitCode != 0 || !File.Exists(output) || new FileInfo(output).Length > 24000)
                throw new InvalidOperationException($"Codex 설명 생성 실패(종료 {process.ExitCode}, 결과 파일 {File.Exists(output)}, {GraphRoleFailure.Code(eventText + errorText)}). 로그인과 실행기 지원을 확인하세요.");
            var data = JsonSerializer.Deserialize<RoleOutput>(await File.ReadAllTextAsync(output, ct), GraphWorker.Json)
                ?? throw new InvalidOperationException("설명 결과를 읽지 못했습니다.");
            return new(projectId, context.Node.NodeId, context.Revision, context.Fingerprint,
                "Codex CLI · 기본 모델(모델 정보 미수집)", data.Claims);
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
    private sealed record RoleOutput(GraphRoleClaim[] Claims);
}
