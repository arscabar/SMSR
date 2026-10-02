using System.Net;
using System.Net.Http;

namespace SMSR.App.Mvp;

internal static class DashboardDisclosureHttpSelfCheck
{
    internal static async Task RunAsync(HttpClient client, string address)
    {
        using var asset = await client.GetAsync(address + "/assets/smsr-dashboard-disclosures.js?v=1");
        var script = await asset.Content.ReadAsStringAsync();
        if (asset.StatusCode != HttpStatusCode.OK || asset.Headers.CacheControl?.NoStore != true
            || asset.Content.Headers.ContentType?.MediaType != "text/javascript"
            || !script.Contains("window.smsrDisclosures", StringComparison.Ordinal)
            || !script.Contains("sessionStorage.setItem", StringComparison.Ordinal)
            || !script.Contains("restore();", StringComparison.Ordinal))
            throw new Exception("Dashboard disclosure script HTTP route or body is invalid");
    }
}
