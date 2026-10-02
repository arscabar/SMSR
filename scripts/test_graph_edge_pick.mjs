import assert from 'node:assert/strict';
import {nearestEdge} from '../src/SMSR.App/WebAssets/graph-explorer-edges.js';
import {svgEdges} from '../src/SMSR.App/WebAssets/graph-explorer-edges.js';
import {Element} from './graph-explorer-drag-fixture.mjs';
const node={label:'n'};
const entries=[
 {a:{x:0,y:0,node},b:{x:300,y:0,node}},
 {a:{x:0,y:4,node},b:{x:300,y:4,node}}
];
// Both 16px hit regions overlap; DOM paint order used to choose the second.
assert.equal(nearestEdge(entries,{x:250,y:27}),entries[0]);
assert.equal(nearestEdge(entries,{x:250,y:31}),entries[1]);
entries[0].b.y=80;
assert.equal(nearestEdge(entries,{x:250,y:31}),entries[1]);
assert.equal(nearestEdge([],{x:0,y:0}),undefined);
globalThis.document={createElementNS:(_,tag)=>new Element(tag)};
const svg=new Element('svg'),selected=[];
const coords=new Map([['a',entries[1].a],['b',entries[1].b],['c',{...entries[1].b,y:8}]]);
const links=[[{sourceId:'a',targetId:'b',relation:'CALLS'}],[{sourceId:'a',targetId:'c',relation:'CALLS'}]];
const update=svgEdges(svg,{coords,links},edges=>selected.push(edges));
const hits=svg.children.filter(child=>child.tag==='line'&&child.getAttribute('role')==='button');
hits[1].event('click',{type:'click',clientX:250,clientY:31});
assert.equal(selected.at(-1),links[0]);
hits[1].event('keydown',{key:'Enter'});assert.equal(selected.at(-1),links[1]);
coords.get('b').y=120;update();
hits[1].event('click',{type:'click',clientX:250,clientY:33});assert.equal(selected.at(-1),links[1]);
svg.getScreenCTM=()=>({inverse:()=>({scale:.5})});
hits[1].event('click',{type:'click',clientX:500,clientY:66});assert.equal(selected.at(-1),links[1]);
console.log('PASS overlapping edge selection and moved geometry');
