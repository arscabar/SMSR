using System.Drawing;
using System.Windows.Forms;

namespace SMSR.App.Infrastructure;

public sealed class TrayStatusIcon : IDisposable
{
    private readonly NotifyIcon _icon = new() { Icon = AppIcon(), Text = "Show Me Status Report", Visible = true };

    public TrayStatusIcon(Action showWindow, Action exitApplication)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("열기", null, (_, _) => showWindow());
        menu.Items.Add("완전 종료", null, (_, _) => exitApplication());
        _icon.ContextMenuStrip = menu;
        _icon.DoubleClick += (_, _) => showWindow();
    }

    public void Dispose() => _icon.Dispose();

    private static Icon AppIcon()
    {
        try { return Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? "") ?? SystemIcons.Information; }
        catch { return SystemIcons.Information; }
    }
}
