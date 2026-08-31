using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SMSR.App.Infrastructure;

internal sealed class TrayStatusIcon : IDisposable
{
    private readonly NotifyIcon _icon = new() { Icon = AppIcon(), Text = "Show Me Status Report", Visible = true };
    private readonly Func<TrayMenuState> _state;
    private readonly ToolStripMenuItem _status = new() { Enabled = false };
    private readonly ToolStripMenuItem _dashboard;
    private readonly ToolStripMenuItem _startServer;
    private readonly ToolStripMenuItem _stopServer;

    public TrayStatusIcon(Action showWindow, Action openDashboard, Action startServer,
        Action stopServer, Action openSettings, Action exitApplication, Func<TrayMenuState> state)
    {
        _state = state;
        var menu = new ContextMenuStrip();
        menu.Items.Add(_status);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("SMSR 열기", null, (_, _) => showWindow());
        _dashboard = new("현재 대시보드 열기", null, (_, _) => openDashboard());
        menu.Items.Add(_dashboard);
        menu.Items.Add(new ToolStripSeparator());
        _startServer = new("서버 시작", null, (_, _) => startServer());
        _stopServer = new("서버 중지", null, (_, _) => stopServer());
        menu.Items.Add(_startServer);
        menu.Items.Add(_stopServer);
        menu.Items.Add("설정 열기", null, (_, _) => openSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("완전 종료", null, (_, _) => exitApplication());
        menu.Opening += (_, _) => RefreshStatus();
        _icon.ContextMenuStrip = menu;
        _icon.DoubleClick += (_, _) => showWindow();
        RefreshStatus();
    }

    public void RefreshStatus()
    {
        var state = _state();
        _status.Text = state.StatusText;
        _dashboard.Enabled = state.CanOpenDashboard;
        _startServer.Enabled = !state.IsServerRunning;
        _stopServer.Enabled = state.IsServerRunning;
        _icon.Text = state.ToolTip;
    }

    public void Dispose() => _icon.Dispose();

    private static Icon AppIcon()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "SMSR.ico");
            return File.Exists(path) ? new Icon(path) : Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? "") ?? SystemIcons.Information;
        }
        catch { return SystemIcons.Information; }
    }
}
