using System.Net;

namespace SMSR.App.Mvp;

internal static class DashboardLiveUpdates
{
    public static string Render(string projectId, string workflowId)
    {
        var project = WebUtility.UrlEncode(projectId);
        var workflow = WebUtility.UrlEncode(workflowId);
        return $$"""
            <script>
            (() => {
              const stream = new EventSource('/api/events/stream?projectId={{project}}&workflowId={{workflow}}');
              let connected = false;
              stream.addEventListener('state', async () => {
                if (!connected) { connected = true; return; }
                try {
                  const response = await fetch(location.href, { cache: 'no-store' });
                  if (!response.ok) return;
                  const next = new DOMParser().parseFromString(await response.text(), 'text/html');
                  document.querySelector('header')?.replaceWith(next.querySelector('header'));
                  document.querySelector('main')?.replaceWith(next.querySelector('main'));
                  const currentAlert = document.querySelector('#alert');
                  const nextAlert = next.querySelector('#alert');
                  if (currentAlert && nextAlert) currentAlert.replaceWith(nextAlert);
                  else if (currentAlert) currentAlert.remove();
                  else if (nextAlert) document.querySelector('main')?.before(nextAlert);
                } catch { }
              });
              document.addEventListener('click', event => {
                const link = event.target.closest?.('.flow-svg a');
                if (!link) return;
                event.preventDefault();
                const now = Date.now();
                const previous = Number(sessionStorage.getItem('smsr-graph-nav') || 0);
                if (now - previous < 600) return;
                sessionStorage.setItem('smsr-graph-nav', String(now));
                location.assign(link.href);
              });
              document.addEventListener('dblclick', event => {
                if (event.target.closest?.('.flow-svg')) event.preventDefault();
              });
            })();
            </script>
            """;
    }
}
