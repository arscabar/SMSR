using System.Diagnostics;
using System.Windows;

namespace SMSR.App.Infrastructure;

public sealed class WindowsPlatformActions : IPlatformActions
{
    public bool TryCopyToClipboard(string value)
    {
        try { System.Windows.Clipboard.SetText(value); return true; }
        catch { return false; }
    }

    public bool TryOpenBrowser(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); return true; }
        catch { return false; }
    }
}
