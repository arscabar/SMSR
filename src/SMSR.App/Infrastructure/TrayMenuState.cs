using System.Drawing;

namespace SMSR.App.Infrastructure;

internal sealed record TrayMenuState(
    bool IsServerRunning, bool IsCodexConnected, bool CanOpenDashboard)
{
    public string StatusText => !IsServerRunning
        ? "● 서버 중지됨"
        : IsCodexConnected ? "● 서버 실행 중 · Codex 연결됨" : "● 서버 실행 중 · Codex 대기 중";

    public string ToolTip => !IsServerRunning
        ? "SMSR · 서버 중지됨"
        : IsCodexConnected ? "SMSR · Codex 연결됨" : "SMSR · 연결 대기 중";

    public Color StatusColor => !IsServerRunning
        ? Color.Firebrick : IsCodexConnected ? Color.SeaGreen : Color.DarkOrange;
}
