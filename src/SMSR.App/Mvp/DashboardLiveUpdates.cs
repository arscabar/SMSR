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
              const replay = input => {
                const step = Number(input.value);
                const items = document.querySelectorAll('#timeline-list > li');
                items.forEach((item, index) => { item.hidden = index !== step - 1; });
                const output = document.getElementById('timeline-position');
                if (output) output.textContent = `${step}/${items.length}`;
              };
              const editContext = field => {
                if (field.classList.contains('context-editing')) return;
                const text = field.querySelector('p'), feedback = field.querySelector('.context-feedback');
                const original = field.dataset.empty === 'true' ? '' : text.textContent;
                const input = document.createElement('textarea');
                input.value = original; input.maxLength = field.dataset.field === 'result' ? 4000 : 2000;
                input.setAttribute('aria-label', field.querySelector('h3').textContent.trim());
                field.classList.add('context-editing'); text.replaceWith(input); input.focus(); input.select();
                let finished = false;
                const restore = message => {
                  input.replaceWith(text); field.classList.remove('context-editing'); feedback.textContent = message;
                };
                const save = async () => {
                  if (finished) return; finished = true;
                  const value = input.value.trim();
                  if (!value || value === original) {restore(value ? '' : '내용을 입력하세요.'); return;}
                  input.disabled = true; feedback.textContent = '저장 중…';
                  const owner = field.closest('.workflow-context');
                  try {
                    const response = await fetch('/api/context', {method:'POST', headers:{'Content-Type':'application/json'},
                      body:JSON.stringify({projectId:owner.dataset.project, workflowId:owner.dataset.workflow,
                        [field.dataset.field]:value})});
                    const data = await response.json();
                    if (!response.ok) throw new Error(data.error || '저장 실패');
                    text.textContent = value; field.dataset.empty = 'false'; restore('저장했습니다.');
                    await refresh();
                  } catch(error) {restore(error.message);}
                };
                input.addEventListener('blur', save);
                input.addEventListener('keydown', event => {
                  if (event.key === 'Escape') {finished = true; restore(''); field.focus();}
                  if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {event.preventDefault(); void save();}
                });
              };
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
                if (document.querySelector('.context-editing')) return;
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
                    if (document.querySelector('.context-editing')) return;
                    const scroll = captureScroll();
                    const timelineInput = document.getElementById('timeline-step');
                    const timelineAtEnd = timelineInput?.value === timelineInput?.max;
                    const timelineStep = Number(timelineInput?.value || 0);
                    saveCardState();
                    document.querySelector('header')?.replaceWith(next.querySelector('header'));
                    setLiveStatus(liveStatus);
                    document.querySelector('main')?.replaceWith(next.querySelector('main'));
                    restoreScroll(scroll);
                    restoreCardState();
                    const currentTimeline = document.getElementById('timeline-step');
                    if (currentTimeline && !timelineAtEnd) { currentTimeline.value = String(Math.min(timelineStep, Number(currentTimeline.max))); replay(currentTimeline); }
                    ['alert', 'integrity-alert'].forEach(id => {
                      const currentAlert = document.getElementById(id);
                      const nextAlert = next.getElementById(id);
                      if (currentAlert && nextAlert) currentAlert.replaceWith(nextAlert);
                      else if (currentAlert) currentAlert.remove();
                      else if (nextAlert) document.querySelector('main')?.before(nextAlert);
                    });
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
                const evidenceLink = event.target.closest?.('.evidence-links a');
                if (evidenceLink) {
                  const input = document.getElementById('timeline-step');
                  const entries = [...document.querySelectorAll('#timeline-list > li')];
                  const selected = entries.findIndex(item => '#' + item.id === evidenceLink.getAttribute('href'));
                  if (input && selected >= 0) { input.value = String(selected + 1); replay(input); }
                  return;
                }
                const action = event.target.closest?.('.node-action');
                if (action) {
                  const panel = action.closest('.node-actions');
                  const buttons = [...(panel?.querySelectorAll('.node-action') || [])];
                  buttons.forEach(button => button.disabled = true);
                  const status = panel?.querySelector('.node-action-status');
                  if (status) { status.textContent = '전달 중…'; window.smsrLoading?.show(status, 'working', '전달 중…'); }
                  void fetch('/api/operator-instruction', {
                    method: 'POST', headers: {'Content-Type': 'application/json'},
                    body: JSON.stringify({projectId: action.dataset.project, workflowId: action.dataset.workflow,
                      nodeId: action.dataset.node, action: action.dataset.action})
                  }).then(async response => {
                    if (status) status.textContent = response.ok ? 'Codex 전달 대기 중' : (await response.json()).error || '전달 실패';
                    if (!response.ok) buttons.forEach(button => button.disabled = false);
                  }).catch(() => { if (status) status.textContent = '전달 실패'; buttons.forEach(button => button.disabled = false); })
                    .finally(() => { if (status) window.smsrLoading?.hide(status); });
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
              document.addEventListener('input', event => {
                if (event.target.matches?.('#timeline-step')) replay(event.target);
              });
              document.addEventListener('keydown', event => {
                const field = event.target.closest?.('.context-field');
                if (field && !event.target.matches('textarea') && (event.key === 'Enter' || event.key === ' ')) {
                  event.preventDefault(); editContext(field);
                }
              });
              document.addEventListener('dblclick', event => {
                const field = event.target.closest?.('.context-field');
                if (field) {event.preventDefault(); editContext(field); return;}
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
