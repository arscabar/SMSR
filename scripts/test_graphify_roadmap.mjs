import assert from 'node:assert/strict';
import {readFileSync, existsSync} from 'node:fs';
import {dirname, resolve} from 'node:path';
import {fileURLToPath} from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const path = resolve(root, 'docs/graphify-capability-roadmap-2026-10-01.html');
const html = readFileSync(path, 'utf8');
const rows = [...html.matchAll(/<tr>([\s\S]*?)<\/tr>/g)]
  .map(m => [...m[1].matchAll(/<td>([\s\S]*?)<\/td>/g)].map(c => c[1]));
const tasks = rows.filter(c => /^F\d-\d+$/.test(c[0] ?? ''));
assert.equal(tasks.length, 42);
const ids = new Set(tasks.map(c => c[0]));
assert.equal(ids.size, tasks.length);
const deps = new Map();
for (const row of tasks) {
  assert.equal(row.length, 5, row[0]);
  assert(row.every(c => c.trim().length > 0), row[0]);
  const predecessors = row[2].match(/F\d-\d+/g) ?? [];
  for (const dep of predecessors) assert(ids.has(dep), `${row[0]}: ${dep}`);
  deps.set(row[0], predecessors);
}
const seen = new Set(), active = new Set();
function visit(id) {
  assert(!active.has(id), `cycle: ${id}`);
  if (seen.has(id)) return;
  active.add(id);
  for (const dep of deps.get(id)) visit(dep);
  active.delete(id);
  seen.add(id);
}
for (const id of ids) visit(id);
const requirements = rows.filter(c => /^R\d{2}$/.test(c[0] ?? ''));
assert.equal(requirements.length, 14);
for (let i = 1; i <= 14; i++) {
  const rowsForId = requirements.filter(c => c[0] === `R${String(i).padStart(2, '0')}`);
  assert.equal(rowsForId.length, 1);
  const refs = rowsForId[0][2].match(/F\d-\d+/g) ?? [];
  assert(refs.length > 0);
  for (const id of refs) assert(ids.has(id), id);
}
const anchors = [...html.matchAll(/\bid="([^"]+)"/g)].map(m => m[1]);
assert.equal(new Set(anchors).size, anchors.length);
for (let i = 0; i < 9; i++) {
  assert(anchors.includes(`f${i}`));
  assert(tasks.some(c => c[0].startsWith(`F${i}-`)));
}
for (const [, href] of html.matchAll(/href="([^"]+)"/g)) {
  if (href.startsWith('https://')) continue;
  if (href.startsWith('#')) assert(anchors.includes(href.slice(1)), href);
  else assert(existsSync(resolve(dirname(path), href)), href);
}
assert(html.includes('F4·F5-5·F6-3·4·6·F7·F8 소스 구현·시험') && html.includes('전체 14개 기능의 완료 보고서가 아닙니다'));
assert(html.includes('현재 실행 앱에 반영·재시작했습니다') && html.includes('기존 저장 그래프는 재색인하지 않았습니다') && html.includes('F6-3·4·6'));
assert(html.includes('원문 질문·프롬프트 로그는 저장하지 않습니다'));
assert(!/<script\b/i.test(html));
console.log('PASS: 9 phases, 42 tasks, 14 requirements, acyclic dependencies, unique anchors, local links, progress/privacy guards');
