using System.Diagnostics;
using System.IO;
using System.Text;

namespace SMSR.App.Services;

internal static class GitAutoIndexHook
{
    private const string Marker = "# SMSR automatic graph index";

    public static string? Register(string executable, string? configPath = null, string? hooksRoot = null)
    {
        configPath ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gitconfig");
        hooksRoot ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMSR", "git-hooks");
        var current = GitConfig(configPath, "--get", "core.hooksPath");
        if (!string.IsNullOrWhiteSpace(current) && !SamePath(current, hooksRoot))
            throw new InvalidOperationException("기존 전역 core.hooksPath가 있어 덮어쓰지 않습니다.");
        var hook = Path.Combine(hooksRoot, "post-commit");
        if (File.Exists(hook) && !IsOwned(hook))
            throw new InvalidOperationException("기존 post-commit 훅이 있어 덮어쓰지 않습니다.");
        var full = Path.GetFullPath(executable);
        if (full.Length < 3 || full[1] != ':') throw new ArgumentException("Windows 실행 파일 경로가 필요합니다.");
        var posix = "/" + char.ToLowerInvariant(full[0]) + full[2..].Replace('\\', '/');
        var quoted = "'" + posix.Replace("'", "'\\''") + "'";
        if (SamePath(current, hooksRoot) && IsOwned(hook)
            && File.ReadAllText(hook).Contains(quoted + " --smsr-git-post-commit", StringComparison.Ordinal)) return null;
        Directory.CreateDirectory(hooksRoot);
        var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmssfff");
        var backup = File.Exists(configPath) ? configPath + ".bak-smsr-git-" + stamp : null;
        if (backup is not null) File.Copy(configPath, backup);
        if (File.Exists(hook)) File.Copy(hook, hook + ".bak-smsr-git-" + stamp);
        File.WriteAllText(hook, "#!/bin/sh\n" + Marker + "\n" + quoted
            + " --smsr-git-post-commit >/dev/null 2>&1 &\n", new UTF8Encoding(false));
        GitConfig(configPath, "core.hooksPath", hooksRoot.Replace('\\', '/'));
        return backup;
    }

    public static bool IsRegistered(string? configPath = null, string? hooksRoot = null)
    {
        configPath ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gitconfig");
        hooksRoot ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMSR", "git-hooks");
        return SamePath(GitConfig(configPath, "--get", "core.hooksPath"), hooksRoot)
            && IsOwned(Path.Combine(hooksRoot, "post-commit"));
    }

    public static void Unregister(string? configPath = null, string? hooksRoot = null)
    {
        configPath ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gitconfig");
        hooksRoot ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMSR", "git-hooks");
        if (!IsRegistered(configPath, hooksRoot)) return;
        GitConfig(configPath, "--unset", "core.hooksPath");
        var hook = Path.Combine(hooksRoot, "post-commit");
        File.Move(hook, hook + ".smsr-disabled-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss"));
    }

    private static bool IsOwned(string path) => File.Exists(path)
        && File.ReadLines(path).Take(3).Any(line => line == Marker);

    private static bool SamePath(string? left, string right)
        => !string.IsNullOrWhiteSpace(left) && string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);

    private static string GitConfig(string configPath, params string[] arguments)
    {
        var start = new ProcessStartInfo("git") { UseShellExecute = false, RedirectStandardOutput = true,
            RedirectStandardError = true, CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8, StandardErrorEncoding = System.Text.Encoding.UTF8 };
        start.Environment["GIT_CONFIG_GLOBAL"] = configPath;
        start.ArgumentList.Add("config"); start.ArgumentList.Add("--global");
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Git을 실행할 수 없습니다.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0 && !(arguments[0] == "--get" && process.ExitCode == 1))
            throw new InvalidOperationException("Git 전역 훅 설정에 실패했습니다: " + error.Trim());
        return output.Trim();
    }
}
