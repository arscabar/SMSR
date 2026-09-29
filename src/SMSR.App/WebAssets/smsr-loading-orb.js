// thinking-orbs 0.3.1 engine adapter; MIT notice: thinking-orbs-LICENSE.txt.
import { MODE_DRAWS, resolvePreset } from './thinking-orbs-engine.js';

const active = new WeakMap();
const motion = matchMedia('(prefers-reduced-motion: reduce)');

function hide(host) {
  const item = active.get(host);
  if (!item) return;
  clearTimeout(item.delay);
  cancelAnimationFrame(item.frame);
  item.observer?.disconnect();
  if (item.resume) {
    document.removeEventListener('visibilitychange', item.resume);
    motion.removeEventListener('change', item.resume);
  }
  item.canvas?.remove();
  active.delete(host);
}

function show(host, state, label) {
  if (!host) return;
  hide(host);
  host.textContent = label;
  const item = { delay: 0, frame: 0, visible: true };
  active.set(host, item);
  item.delay = setTimeout(() => {
    if (active.get(host) !== item) return;
    const canvas = document.createElement('canvas');
    const context = canvas.getContext('2d');
    if (!context) return;
    const size = 20, dpr = Math.min(2, devicePixelRatio || 1);
    canvas.width = Math.round(size * dpr);
    canvas.height = Math.round(size * dpr);
    canvas.style.cssText = 'width:20px;height:20px;margin-right:7px;vertical-align:-5px';
    canvas.setAttribute('aria-hidden', 'true');
    host.prepend(canvas);
    item.canvas = canvas;
    const { mode, speed, opts } = resolvePreset(state, size);
    const draw = MODE_DRAWS[mode];
    const dark = getComputedStyle(document.documentElement).colorScheme.includes('dark');
    const paint = seconds => {
      context.setTransform(dpr, 0, 0, dpr, 0, 0);
      context.clearRect(0, 0, size, size);
      draw(context, size, seconds * speed, dark, opts);
    };
    const tick = now => { paint(now / 1000); item.frame = requestAnimationFrame(tick); };
    item.resume = () => {
      cancelAnimationFrame(item.frame);
      if (motion.matches) { paint(0.6); return; }
      if (item.visible && !document.hidden) item.frame = requestAnimationFrame(tick);
    };
    if (typeof IntersectionObserver !== 'undefined') {
      item.observer = new IntersectionObserver(([entry]) => {
        item.visible = entry.isIntersecting;
        item.resume();
      });
      item.observer.observe(canvas);
    }
    document.addEventListener('visibilitychange', item.resume);
    motion.addEventListener('change', item.resume);
    item.resume();
  }, 250);
}

window.smsrLoading = { show, hide };
