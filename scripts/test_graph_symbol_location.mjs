import assert from 'node:assert/strict';
import {Element} from './graph-explorer-drag-fixture.mjs';
import {svgNode,heightOf} from '../src/SMSR.App/WebAssets/graph-explorer-svg.js';
globalThis.document={createElementNS:(_,tag)=>new Element(tag)};
const text=e=>[e.textContent||'',...e.children.map(text)].join(' ');
const svg=new Element('svg'),nodes=['first','second'].map((folder,index)=>({nodeId:'symbol:'+folder,kind:'symbol',label:'Run',sourcePath:folder+'/Service.cs',line:index+10}));
for(const node of nodes){
  const group=svgNode(svg,node,20,20,false,()=>{});
  assert.match(text(group),new RegExp(node.sourcePath+':'+node.line));
  assert.match(group.getAttribute('aria-label'),new RegExp(node.sourcePath));
  assert.ok(heightOf(node)>heightOf({label:'Run'}),'location lines require their own card height');
}
assert.notEqual(svg.children[0].getAttribute('aria-label'),svg.children[1].getAttribute('aria-label'));
console.log('PASS same-name symbols show distinct source location and accessible labels');
