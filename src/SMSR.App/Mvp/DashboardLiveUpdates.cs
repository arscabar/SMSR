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
              let refreshing = false;
              let queued = false;
              const scrollIds = ['flow', 'graph', 'details'];
              const cardStateKey = 'smsr-status-cards:{{project}}:{{workflow}}';
              const captureScroll = () => new Map(scrollIds.map(id => {
                const element = document.getElementById(id);
                return [id, { top: element?.scrollTop || 0, left: element?.scrollLeft || 0 }];
              }));
              const restoreScroll = positions => positions.forEach((position, id) => {
                const element = document.getElementById(id);
                if (!element) return;
                element.scrollTop = position.top;
                element.scrollLeft = position.left;
              });
              const readCardState = () => {
                try { return JSON.parse(sessionStorage.getItem(cardStateKey) || 'null'); } catch { return null; }
              };
              const updateCardButton = () => {
                const cards = [...document.querySelectorAll('.status-card')];
                const button = document.getElementById('toggle-status-cards');
                if (button) button.textContent = cards.length && cards.every(card => !card.open) ? '전체 펼치기' : '전체 접기';
              };
              const saveCardState = () => {
                const cards = [...document.querySelectorAll('.status-card')];
                sessionStorage.setItem(cardStateKey, JSON.stringify({
                  allCollapsed: cards.length > 0 && cards.every(card => !card.open),
                  values: Object.fromEntries(cards.map(card => [card.dataset.recordId, card.open]))
                }));
                updateCardButton();
              };
              const restoreCardState = () => {
                const state = readCardState();
                if (state) document.querySelectorAll('.status-card').forEach(card => {
                  card.open = state.allCollapsed ? false : state.values?.[card.dataset.recordId] ?? true;
                });
                updateCardButton();
              };
              const refresh = async () => {
                if (refreshing) { queued = true; return; }
                refreshing = true;
                do {
                  queued = false;
                  try {
                    const response = await fetch(location.href, { cache: 'no-store' });
                    if (!response.ok) continue;
                    const next = new DOMParser().parseFromString(await response.text(), 'text/html');
                    const scroll = captureScroll();
                    saveCardState();
                    document.querySelector('header')?.replaceWith(next.querySelector('header'));
                    document.querySelector('main')?.replaceWith(next.querySelector('main'));
                    restoreScroll(scroll);
                    restoreCardState();
                    const currentAlert = document.querySelector('#alert');
                    const nextAlert = next.querySelector('#alert');
                    if (currentAlert && nextAlert) currentAlert.replaceWith(nextAlert);
                    else if (currentAlert) currentAlert.remove();
                    else if (nextAlert) document.querySelector('main')?.before(nextAlert);
                  } catch { }
                } while (queued);
                refreshing = false;
              };
              stream.addEventListener('state', () => {
                if (!connected) { connected = true; return; }
                void refresh();
              });
              document.addEventListener('click', event => {
                const toggle = event.target.closest?.('#toggle-status-cards');
                if (toggle) {
                  const cards = [...document.querySelectorAll('.status-card')];
                  const open = cards.length > 0 && cards.every(card => !card.open);
                  cards.forEach(card => card.open = open);
                  saveCardState();
                  return;
                }
                const link = event.target.closest?.('.flow-svg a');
                if (!link) return;
                event.preventDefault();
                const now = Date.now();
                const previous = Number(sessionStorage.getItem('smsr-graph-nav') || 0);
                if (now - previous < 600) return;
                const target = link.getAttribute('href');
                if (!target) return;
                sessionStorage.setItem('smsr-graph-nav', String(now));
                location.assign(target);
              });
              document.addEventListener('dblclick', event => {
                if (event.target.closest?.('.flow-svg')) event.preventDefault();
              });
              document.addEventListener('toggle', event => {
                if (event.target.matches?.('.status-card')) saveCardState();
              }, true);
              restoreCardState();
            })();
            </script>
            """;
    }
}
