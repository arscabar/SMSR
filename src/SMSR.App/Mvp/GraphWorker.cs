using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace SMSR.App.Mvp;

public sealed class GraphWorker
{
    // ponytail: one local model/engine process at a time; raise only after memory measurements.
    private readonly SemaphoreSlim _gate = new(1, 1);
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static string PythonPath => Path.Combine(Environment.GetFolderPath(
        Environment.SpecialFolder.LocalApplicationData), "SMSR", "graph-runtime", "Scripts", "python.exe");
    private static string CSharpPath
    {
        get
        {
            var folder = Path.Combine(AppContext.BaseDirectory, "CSharpAnalysis");
            var selfContained = Path.Combine(folder, "win-x64", "SMSR.CSharpAnalysis.exe");
            return File.Exists(selfContained) ? selfContained : Path.Combine(folder, "SMSR.CSharpAnalysis.exe");
        }
    }

    public async Task<JsonElement> RunAsync(object request, CancellationToken ct = default)
        => await RunLocalAsync(request, PythonPath, [Path.Combine(AppContext.BaseDirectory, "GraphRuntime", "worker.py")], ct);

    internal Task<JsonElement> RunCSharpAsync(object request, CancellationToken ct) => RunLocalAsync(request, CSharpPath, [], ct);

    internal Task<JsonElement> RunCSharpIndexAsync(object request, CancellationToken ct) => RunLocalAsync(request,
        CSharpPath, ["--index"], ct);

    private async Task<JsonElement> RunLocalAsync(object request, string executable, string[] arguments, CancellationToken ct)
    {
        var input = JsonSerializer.SerializeToUtf8Bytes(request, Json);
        if (input.Length > 32 * 1024 * 1024) throw new ArgumentException("분석 입력은 32 MiB 이하여야 합니다.");
        if (!File.Exists(executable)) throw new InvalidOperationException("로컬 분석 도구가 없습니다. Python 도구 또는 C# 분석기 포함 배포본을 확인하세요.");
        await _gate.WaitAsync(ct);
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
            deadline.CancelAfter(TimeSpan.FromMinutes(2));
            var start = new ProcessStartInfo(executable)
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
            };
            foreach (var argument in arguments) start.ArgumentList.Add(argument);
            start.Environment["HF_HUB_OFFLINE"] = "1";
            start.Environment["HF_HUB_DISABLE_TELEMETRY"] = "1";
            using var job = new GraphLspJob();
            using var process = Process.Start(start) ?? throw new InvalidOperationException("분석 도구 실행 실패");
            using var registration = deadline.Token.Register(() => { job.Dispose(); try { process.Kill(true); } catch (InvalidOperationException) { } });
            try { job.Assign(process); }
            catch { process.Kill(true); throw; }
            var output = GraphProcessOutput.ReadAsync(process.StandardOutput.BaseStream, deadline.Token);
            var errors = process.StandardError.BaseStream.CopyToAsync(Stream.Null, deadline.Token);
            await process.StandardInput.BaseStream.WriteAsync(input, deadline.Token);
            process.StandardInput.Close();
            await process.WaitForExitAsync(deadline.Token);
            var text = await output;
            await errors;
            if (process.ExitCode != 0 || text.Length > 16 * 1024 * 1024)
                throw new InvalidOperationException("로컬 분석 실패: 입력·설치·모델·지원 범위·실행 제한을 확인하세요.");
            return JsonSerializer.Deserialize<JsonElement>(text);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { throw new InvalidOperationException("로컬 분석이 2분 제한을 초과했습니다. 범위를 줄이세요."); }
        finally { _gate.Release(); }
    }
}
