import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';

class Control {
  constructor(value=''){this.value=value;this.handlers={};}
  addEventListener(name,handler){this.handlers[name]=handler;}
}
const controls=new Map(['relation-direction','relation-kind','relation-previous','relation-next','relation-page','relation-count','graph-tools','relation-expand'].map(id=>['#'+id,new Control()]));
controls.get('#relation-direction').value='both';
globalThis.document={querySelector:id=>controls.get(id)};
const code=await readFile(new URL('../src/SMSR.App/WebAssets/graph-explorer-relations.js',import.meta.url),'utf8');
const {relations}=await import('../src/SMSR.App/WebAssets/graph-explorer-relations.js');
const requests=[],renders=[],messages=[];
let visible=null,clears=0;
let resolve;
const widget=relations({projectId:'test',message:value=>messages.push(value),clearGraph:()=>{visible=null;clears++;},render:data=>{visible=data;renders.push(data);},get:async(path,args)=>{
  requests.push({path,args});
  if(args.nodeId==='slow')return new Promise(r=>{resolve=r});
  if(args.relation==='BROKEN')throw new Error('관계 조회 실패');
  return {node:{nodeId:args.nodeId},items:[{incoming:true,node:{nodeId:'leaf'},edge:{sourceId:'leaf',targetId:args.nodeId}}],total:121,revision:7,direction:args.direction,relation:args.relation};
}});
await widget.select({nodeId:'hub'});
assert.equal(requests[0].args.offset,'0');assert.equal(renders[0].incoming.length,1);
controls.get('#relation-next').handlers.click();await new Promise(setImmediate);
assert.equal(requests.at(-1).args.offset,'20');assert.equal(requests.at(-1).args.revision,'7');
controls.get('#relation-kind').value='CALLS';controls.get('#relation-kind').handlers.change();await new Promise(setImmediate);
assert.equal(requests.at(-1).args.offset,'0');assert.equal(requests.at(-1).args.relation,'CALLS');
controls.get('#relation-expand').handlers.click();await new Promise(setImmediate);
assert.equal(requests.at(-1).args.nodeId,'leaf');assert.equal(requests.at(-1).args.relation,'CALLS');
assert.equal(renders.at(-1).nodes.length,2);
assert.ok(visible);
controls.get('#relation-kind').value='BROKEN';controls.get('#relation-kind').handlers.change();await new Promise(setImmediate);
assert.equal(visible,null);assert.equal(messages.at(-1),'관계 조회 실패');
assert.equal(controls.get('#relation-count').textContent,'');assert.equal(controls.get('#relation-page').textContent,'–');
assert.equal(controls.get('#graph-tools').hidden,false);assert.ok(clears>=4);
controls.get('#relation-kind').value='';
controls.get('#relation-kind').handlers.change();await new Promise(setImmediate);assert.ok(visible,'query tools must allow recovery after failure');
const slow=widget.select({nodeId:'slow'});const before=renders.length;
widget.clear();resolve({node:{nodeId:'slow'},items:[],total:0,revision:7});await slow;
assert.equal(renders.length,before);
console.log('Relation pagination/filter/revision/late response checks OK');
