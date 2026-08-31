using Microsoft.Win32;
using System.IO;

namespace SMSR.App.Services;

internal sealed class WindowsStartupRegistration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SMSR";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return string.Equals(key?.GetValue(ValueName) as string, CurrentCommand(), StringComparison.OrdinalIgnoreCase);
    }

    public void Enable()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, true);
        key.SetValue(ValueName, CurrentCommand(), RegistryValueKind.String);
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
        key?.DeleteValue(ValueName, false);
    }

    internal static string BuildCommand(string executable) => $"\"{Path.GetFullPath(executable)}\" --background";
    private static string CurrentCommand() => BuildCommand(Environment.ProcessPath
        ?? throw new InvalidOperationException("SMSR 실행 파일 경로를 확인할 수 없습니다."));
}
