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
              let navigating = false;
              let refreshController = null;
              let liveStatus = '자동 갱신 연결 중';
              const scrollIds = ['flow', 'graph', 'details'];
              const cardStateKey = 'smsr-status-cards:{{project}}:{{workflow}}';
              const showLiveStatus = value => {
                const element = document.getElementById('live-connection');
                if (element && element.dataset.static !== 'true') element.textContent = value;
              };
              const setLiveStatus = value => { liveStatus = value; showLiveStatus(value); };
              const updateRunningTimes = () => document.querySelectorAll('.running-time').forEach(element => {
                const seconds = Math.max(0, Math.floor((Date.now() - Number(element.dataset.start)) / 1000));
                element.textContent = `실행 중 · ${seconds}초`;
              });
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
                showLiveStatus('새 정보 반영 중…');
                try {
                  do {
                    queued = false;
                    const controller = new AbortController();
                    refreshController = controller;
                    const timeout = setTimeout(() => controller.abort(), 8000);
                    try {
                    const response = await fetch(location.href, { cache: 'no-store', signal: controller.signal });
                    if (!response.ok) continue;
                    const next = new DOMParser().parseFromString(await response.text(), 'text/html');
                    const scroll = captureScroll();
                    saveCardState();
                    document.querySelector('header')?.replaceWith(next.querySelector('header'));
                    setLiveStatus(liveStatus);
                    document.querySelector('main')?.replaceWith(next.querySelector('main'));
                    restoreScroll(scroll);
                    restoreCardState();
                    const currentAlert = document.querySelector('#alert');
                    const nextAlert = next.querySelector('#alert');
                    if (currentAlert && nextAlert) currentAlert.replaceWith(nextAlert);
                    else if (currentAlert) currentAlert.remove();
                    else if (nextAlert) document.querySelector('main')?.before(nextAlert);
                    } catch { }
                    finally { clearTimeout(timeout); if (refreshController === controller) refreshController = null; }
                  } while (queued);
                } finally { refreshing = false; if (!navigating) showLiveStatus(liveStatus); }
              };
              stream.addEventListener('state', () => {
                if (navigating) return;
                if (!connected) { connected = true; return; }
                void refresh();
              });
              stream.addEventListener('open', () => setLiveStatus('자동 갱신 연결됨'));
              stream.addEventListener('error', () => setLiveStatus('자동 갱신 재연결 중'));
              document.addEventListener('click', event => {
                const action = event.target.closest?.('.node-action');
                if (action) {
                  const panel = action.closest('.node-actions');
                  const buttons = [...(panel?.querySelectorAll('.node-action') || [])];
                  buttons.forEach(button => button.disabled = true);
                  const status = panel?.querySelector('.node-action-status');
                  if (status) status.textContent = '전달 중…';
                  void fetch('/api/operator-instruction', {
                    method: 'POST', headers: {'Content-Type': 'application/json'},
                    body: JSON.stringify({projectId: action.dataset.project, workflowId: action.dataset.workflow,
                      nodeId: action.dataset.node, action: action.dataset.action})
                  }).then(async response => {
                    if (status) status.textContent = response.ok ? 'Codex 전달 대기 중' : (await response.json()).error || '전달 실패';
                    if (!response.ok) buttons.forEach(button => button.disabled = false);
                  }).catch(() => { if (status) status.textContent = '전달 실패'; buttons.forEach(button => button.disabled = false); });
                  return;
                }
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
                if (navigating) return;
                const target = link.getAttribute('href');
                if (!target) return;
                navigating = true;
                queued = false;
                refreshController?.abort();
                stream.close();
                showLiveStatus('선택 작업 여는 중…');
                location.assign(target);
              });
              document.addEventListener('dblclick', event => {
                if (event.target.closest?.('.flow-svg')) event.preventDefault();
              });
              document.addEventListener('toggle', event => {
                if (event.target.matches?.('.status-card')) saveCardState();
              }, true);
              restoreCardState();
              updateRunningTimes();
              setInterval(updateRunningTimes, 1000);
            })();
            </script>
            """;
    }
}
