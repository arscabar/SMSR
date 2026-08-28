namespace SMSR.App.Mvp;

internal static class DashboardPalette
{
    private const string Dark = """
        :root{color-scheme:dark;--bg:#10151f;--panel:#151c29;--surface:#1a2332;--graph:#141b27;--text:#e8edf6;--muted:#9ba8bc;--border:#293243;--border2:#2c374a;--pending:#283347;--pending-stroke:#677892;--success:#173d2b;--active:#1d3f68;--validating:#38275d;--error:#51232c}
        """;
    private const string Light = """
        :root{color-scheme:light;--bg:#f6f7fb;--panel:#fff;--surface:#f7f8fc;--graph:#fff;--text:#172033;--muted:#667085;--border:#dce3ef;--border2:#cbd5e1;--pending:#eef1f6;--pending-stroke:#7c879a;--success:#e4f7ee;--active:#e7f1ff;--validating:#f0eaff;--error:#ffe9ec}
        """;

    public static string Resolve(string? theme) => theme is "Light" or "밝은 테마" ? Light : Dark;
}
