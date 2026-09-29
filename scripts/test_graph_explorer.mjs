import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

class Element {
  constructor(tag) { this.tag = tag; this.children = []; this.attrs = {}; this.textContent = ''; }
  append(...children) { this.children.push(...children); }
  replaceChildren(...children) { this.children = children; this.textContent = ''; }
  setAttribute(key, value) { this.attrs[key] = String(value); }
  addEventListener() {}
}
const note = new Element('p');
globalThis.document = {
  querySelector: selector => selector === '#graph-note' ? note : null,
  createElement: tag => new Element(tag), createElementNS: (_, tag) => new Element(tag)
};
const code = await readFile(new URL('../src/SMSR.App/WebAssets/graph-explorer-flow.js', import.meta.url), 'utf8');
const { showFlow } = await import('data:text/javascript,' + encodeURIComponent(code));
const graph = new Element('div'), edges = new Element('ol'), details = new Element('div');
const messages = [];
const message = value => messages.push(value);
await showFlow('SMSR', {kind:'document',sourcePath:'README.md'}, graph, edges, details, message);
assert.match(graph.textContent, /코드 파일/);
assert.equal(details.children.length, 0);

globalThis.fetch = async (_, options) => {
  assert.equal(JSON.parse(options.body).input, 'src/main.py');
  return {ok:true,json:async()=>({stale:false,report:{result:{statements:[
    {id:'a',scope:1,line:2,kind:'assignment_statement'},
    {id:'b',scope:1,line:3,kind:'return_statement'}],
    edges:[{source:'a',target:'b',relation:'DATA_MAY_DEPEND',variable:'value'}]}}})};
};
await showFlow('SMSR', {kind:'code',sourcePath:'src/main.py'}, graph, edges, details, message);
assert.equal(graph.children[0].tag, 'svg');
assert.match(edges.children[0].textContent, /2줄 → 3줄/);
assert.match(note.textContent, /보수적 의존/);
assert.equal(details.children[0].textContent, '분석·저장');
console.log('graph explorer flow checks passed');
