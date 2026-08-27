using System.IO;
using System.Net.Http;

namespace SMSR.App.Services;

public static class SseStateClient
{
    public static async Task ListenAsync(string url, Func<Task> onState, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken));
        var stateChanged = false;
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null) return;
            if (line == "event: state") stateChanged = true;
            if (line.Length == 0 && stateChanged)
            {
                stateChanged = false;
                await onState();
            }
        }
    }
}
