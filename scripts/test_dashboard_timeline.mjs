import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const source = await readFile(new URL('../src/SMSR.App/Mvp/DashboardLiveUpdates.cs', import.meta.url), 'utf8');
const start = source.indexOf('const replay = input => {');
const end = source.indexOf('const captureScroll =', start);
assert.ok(start >= 0 && end > start);
const items = [{hidden:false},{hidden:false},{hidden:false}], output = {textContent:''};
const document = {querySelectorAll: () => items, getElementById: () => output};
const replay = new Function('document', source.slice(start,end) + 'return replay;')(document);
replay({value:'2'});
assert.deepEqual(items.map(item => item.hidden), [true,false,true]);
assert.equal(output.textContent, '2/3');
replay({value:'3'});
assert.deepEqual(items.map(item => item.hidden), [true,true,false]);
console.log('dashboard timeline: OK');
