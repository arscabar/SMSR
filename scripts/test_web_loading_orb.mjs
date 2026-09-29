import assert from 'node:assert/strict';

const canvases = [];
const listeners = {};
const motion = { matches: true, addEventListener() {}, removeEventListener() {} };
const context = Object.fromEntries(
  ['setTransform', 'clearRect', 'beginPath', 'arc', 'fill', 'moveTo', 'lineTo', 'stroke'].map(name => [name, () => {}]));
globalThis.window = {};
globalThis.document = {
  hidden: false, documentElement: {},
  createElement: () => { const canvas = { style: {}, attrs: {}, setAttribute(name, value) { this.attrs[name] = value; }, getContext: () => context, remove() { this.removed = true; } }; canvases.push(canvas); return canvas; },
  addEventListener(name, callback) { listeners[name] = callback; }, removeEventListener(name) { delete listeners[name]; }
};
globalThis.matchMedia = () => motion;
globalThis.getComputedStyle = () => ({ colorScheme: 'dark' });
globalThis.devicePixelRatio = 1;
globalThis.requestAnimationFrame = () => 1;
globalThis.cancelAnimationFrame = () => {};
globalThis.IntersectionObserver = class { observe() {} disconnect() {} };

await import('../src/SMSR.App/WebAssets/smsr-loading-orb.js');
const host = { textContent: '', prepend(canvas) { this.canvas = canvas; } };
window.smsrLoading.show(host, 'searching', '검색 중…');
window.smsrLoading.hide(host);
await new Promise(resolve => setTimeout(resolve, 300));
assert.equal(canvases.length, 0, '빠른 요청에는 오브가 나타나지 않아야 한다');

window.smsrLoading.show(host, 'connecting', '색인 중…');
await new Promise(resolve => setTimeout(resolve, 300));
assert.equal(host.textContent, '색인 중…');
assert.equal(canvases.length, 1, '긴 요청에는 오브가 나타나야 한다');
assert.equal(host.canvas.attrs['aria-hidden'], 'true');
window.smsrLoading.hide(host);
assert.equal(host.canvas.removed, true, '종료 시 오브를 제거해야 한다');

let frames = 0;
motion.matches = false;
globalThis.requestAnimationFrame = () => ++frames;
window.smsrLoading.show(host, 'solving', '분석 중…');
await new Promise(resolve => setTimeout(resolve, 300));
assert.ok(frames > 0, '일반 모드에서는 애니메이션을 시작해야 한다');
document.hidden = true;
const pausedAt = frames;
listeners.visibilitychange();
assert.equal(frames, pausedAt, '숨긴 탭에서는 새 프레임을 만들지 않아야 한다');
window.smsrLoading.hide(host);
console.log('loading orb checks passed');
