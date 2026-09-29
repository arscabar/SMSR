import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const source = await readFile(new URL('../src/SMSR.App/WebAssets/graph-explorer-labels.js', import.meta.url), 'utf8');
const { labelLines, kindName, relationName } = await import('data:text/javascript,' + encodeURIComponent(source));
const name = 'graph-jdt-verification-2026-09-29.md';
assert.equal(labelLines(name).join(''), name);
assert.ok(labelLines(name).length > 1);
assert.equal(labelLines('한글 문서 제목').join(''), '한글 문서 제목');
assert.ok(labelLines('name: smsr-tracking description: Push a Codex task').some(line => line.includes('description:')));
assert.equal(kindName('heading'), '문서 내부 제목');
assert.equal(kindName('audio'), '음원');
assert.equal(kindName('video'), '영상');
assert.equal(relationName('CONTAINS'), '포함');
assert.equal(relationName('UNKNOWN'), 'UNKNOWN');
console.log('graph explorer labels: OK');
