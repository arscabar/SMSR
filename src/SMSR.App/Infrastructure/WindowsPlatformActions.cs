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

    public bool TryOpenPath(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); return true; }
        catch { return false; }
    }

    public bool Confirm(string title, string message)
        => System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning,
            MessageBoxResult.No) == MessageBoxResult.Yes;
}
