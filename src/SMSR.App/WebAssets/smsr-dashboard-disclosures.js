/* Native disclosure preference survives streamed DOM replacement and reloads. */
(() => {
  const owner = document.querySelector('.workflow-context');
  const key = `smsr-disclosures:${encodeURIComponent(owner?.dataset.project || '')}:${encodeURIComponent(owner?.dataset.workflow || '')}`;
  const selector = 'details[data-disclosure]';
  let state = {};
  const capture = () => {
    state = Object.fromEntries([...document.querySelectorAll(selector)].map(item => [item.dataset.disclosure, item.open]));
    try { sessionStorage.setItem(key, JSON.stringify(state)); } catch { }
  };
  const restore = () => {
    try { state = JSON.parse(sessionStorage.getItem(key) || '{}') || {}; } catch { }
    document.querySelectorAll(selector).forEach(item => {
      const saved = state[item.dataset.disclosure];
      item.open = typeof saved === 'boolean' ? saved : item.dataset.defaultOpen === 'true';
    });
  };
  document.addEventListener('toggle', event => {
    if (event.target.matches?.(selector)) capture();
  }, true);
  window.smsrDisclosures = { capture, restore };
  restore();
})();
