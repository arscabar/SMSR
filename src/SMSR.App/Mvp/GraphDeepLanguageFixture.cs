using System.Diagnostics;

namespace SMSR.App.Mvp;

internal static class GraphDeepLanguageFixture
{
    internal static readonly Dictionary<string, string> Sources = new() {
        ["Lib.cs"] = "public static class Lib\n{\n public static int Twice(int x) => x + x;\n public static int Run(int x) => Twice(x);\n}\n",
        ["Lib.java"] = "public final class Lib {\n static int twice(int x) { return x + x; }\n static int run(int x) { return twice(x); }\n}\n",
        ["lib.ts"] = "function twice(x: number) { return x + x; }\nfunction run(x: number) { return twice(x); }\n",
        ["lib.py"] = "def outer(x):\n def inner(y):\n  return y + y\n return inner(x)\n"
    };
    internal static async Task InitializeAsync(string root)
    {
        using var git = Process.Start(new ProcessStartInfo("git") {
            ArgumentList = { "-C", root, "init", "-q" }, UseShellExecute = false, CreateNoWindow = true
        }) ?? throw new InvalidOperationException("Git 실행 실패");
        await git.WaitForExitAsync();
        if (git.ExitCode != 0) throw new InvalidOperationException("Git 초기화 실패");
    }
}
