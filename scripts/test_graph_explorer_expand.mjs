import assert from 'node:assert/strict';
class Control {addEventListener(name,handler){this[name]=handler;}}
const button=new Control();globalThis.document={querySelector:()=>button};
const {expansion}=await import('../src/SMSR.App/WebAssets/graph-explorer-expand.js');
const item=(id,from='root')=>({node:{nodeId:id},edge:{sourceId:from,targetId:id,relation:'CALLS'}});
const renders=[],messages=[],requests=[];
let mode='normal',release;
const widget=expansion({projectId:'test',render:d=>renders.push(d),message:m=>messages.push(m),get:async(path,args)=>{
  requests.push(args);
  if(mode==='slow')return new Promise(r=>{release=r});
  return {revision:mode==='stale'?8:7,total:40,
    items:Array.from({length:20},(_,i)=>item(`${args.nodeId}-${i}`,args.nodeId))};
}});
const base={node:{nodeId:'root'},items:Array.from({length:20},(_,i)=>item('n'+i)),revision:7,direction:'outgoing',relation:'CALLS'};
widget.reset(base);await button.click();
assert.equal(renders[0].nodes.length,80);assert.ok(renders[0].edges.length<=200);
assert.ok(messages.at(-1).includes('일부 관계'));assert.equal(requests[0].relation,'CALLS');
mode='stale';widget.reset({...base,items:[item('one')]});const before=renders.length;await button.click();
assert.equal(renders.length,before);assert.match(messages.at(-1),/색인이 변경/);
mode='slow';widget.reset({...base,items:[item('one')]});const pending=button.click();widget.clear();
release({revision:7,total:0,items:[]});await pending;assert.equal(renders.length,before);
console.log('Expansion node/edge caps, filter, stale revision and cancelled response OK');
