using System.Diagnostics;
using System.Net.Http;

namespace SMSR.App.Services;

internal static class DashboardProcessLauncher
{
    public static async Task<bool> EnsureStartedAsync(CancellationToken cancellationToken = default)
    {
        if (await IsReadyAsync(cancellationToken)) return true;
        if (!MainInstanceGuard.IsRunning()) StartDashboard();
        for (var attempt = 0; attempt < 15; attempt++)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            if (await IsReadyAsync(cancellationToken)) return true;
        }
        return false;
    }

    internal static ProcessStartInfo CreateStartInfo(string executable)
    {
        var info = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        info.ArgumentList.Add("--background");
        info.ArgumentList.Add("--ensure-server");
        return info;
    }

    private static void StartDashboard()
    {
        var executable = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(executable)) Process.Start(CreateStartInfo(executable));
    }

    private static async Task<bool> IsReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
            using var response = await client.GetAsync(
                $"http://127.0.0.1:{Mvp.LocalServer.Port}/api/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException) { return false; }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { return false; }
    }
}
