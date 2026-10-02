import assert from 'node:assert/strict';
import {Element} from './graph-explorer-drag-fixture.mjs';
import {svgEdges} from '../src/SMSR.App/WebAssets/graph-explorer-edges.js';
import {edgeStyle} from '../src/SMSR.App/WebAssets/graph-explorer-edge-style.js';
globalThis.document={createElementNS:(_,tag)=>new Element(tag)};
const a={nodeId:'a',label:'A'},b={nodeId:'b',label:'B'};
const coords=new Map([['a',{x:20,y:100,node:a}],['b',{x:340,y:100,node:b}]]);
const edge=(relation,confidence='EXTRACTED',resolution='RESOLVED')=>({sourceId:'a',targetId:'b',relation,confidence,resolution});
const links=[[edge('CALLS')],[edge('REFERENCES')],[edge('CALLS','INFERRED')],[{...edge('CALLS'),sourceId:'b',targetId:'a'}]];
const svg=new Element('svg');let selected;
const update=svgEdges(svg,{coords,links},value=>{selected=value;});
const hits=svg.children.filter(e=>e.tag==='line'&&e.getAttribute('role')==='button');
const lines=svg.children.filter(e=>e.tag==='line'&&!e.getAttribute('role'));
const captions=svg.children.filter(e=>e.tag==='text');
assert.equal(new Set(lines.map(e=>e.getAttribute('y1'))).size,4,'same-pair relation kinds and certainty need separate geometry');
for(const [index,hit] of hits.entries()){
  hit.event('click',{type:'click',clientX:280,clientY:Number(hit.getAttribute('y1'))});assert.equal(selected,links[index]);
  captions[index].event('keydown',{key:'Enter'});assert.equal(selected,links[index]);
}
assert.match(captions[0].textContent,/원문 근거/);assert.match(captions[2].textContent,/추론/);
assert.equal(lines[0].style.strokeDasharray,'none');assert.equal(lines[2].style.strokeDasharray,'8 5');
assert.match(hits[2].getAttribute('aria-label'),/추론한 관계/);
assert.equal(edgeStyle(edge('CALLS','EXTRACTED','INFERRED')).label,'대상 추론');
assert.equal(edgeStyle(edge('CALLS','AMBIGUOUS')).strokeDasharray,'2 5');
const before=captions[0].getAttribute('y');coords.get('b').y+=60;update();assert.notEqual(captions[0].getAttribute('y'),before);
console.log('PASS parallel relation/evidence geometry, pointer/keyboard selection, and moving labels');
