using System.Windows.Media;

namespace SMSR.App.Services;

public static class AppThemeService
{
    public static void Apply(string? theme)
    {
        var dark = DashboardThemes.Normalize(theme) == DashboardThemes.Dark;
        Set("CanvasBrush", dark ? "#10151F" : "#F6F7FB");
        Set("SurfaceBrush", dark ? "#151C29" : "#FFFFFF");
        Set("BorderBrush", dark ? "#293243" : "#E4E7EF");
        Set("TextBrush", dark ? "#E8EDF6" : "#20232F");
        Set("MutedTextBrush", dark ? "#9BA8BC" : "#777D8E");
        Set("AccentBrush", dark ? "#62ADFF" : "#5B5CE2");
        Set("AccentHoverBrush", dark ? "#3D8FE5" : "#4849C5");
        Set("AccentSoftBrush", dark ? "#1E3048" : "#ECECFF");
        Set("QuietBrush", dark ? "#202C3F" : "#ECEEF6");
        Set("QuietHoverBrush", dark ? "#2B3A50" : "#DEE2EE");
    }

    private static void Set(string key, string color)
        => System.Windows.Application.Current.Resources[key] =
            new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
}
