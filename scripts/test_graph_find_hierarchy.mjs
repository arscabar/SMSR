import assert from 'node:assert/strict';
class Element{
  constructor(tag){this.tag=tag;this.children=[];this.textContent='';this.events={};this.isConnected=true;}
  append(...items){for(const item of items){item.parent=this;this.children.push(item);}}
  replaceChildren(...items){this.children.forEach(e=>{e.isConnected=false});this.children=[];this.append(...items);}
  addEventListener(event,fn){this.events[event]=fn;}
  setAttribute(key,value){this[key]=value;}
  remove(){this.isConnected=false;if(this.parent)this.parent.children=this.parent.children.filter(e=>e!==this);}
}
globalThis.document={createElement:tag=>new Element(tag)};
const {symbolTree}=await import('../src/SMSR.App/WebAssets/graph-explorer-symbols.js');
const {roleJob}=await import('../src/SMSR.App/WebAssets/graph-explorer-role-job.js');
const collect=e=>[e.textContent,...e.children.map(collect)].join(' ');
const node={nodeId:'file:a.java',kind:'code'},type={nodeId:'symbol:type',kind:'symbol',label:'Switches',line:1,details:{entityKind:'class'}};
let calls=0,chosen;
const box=new Element('div');symbolTree(node,box,{projectId:'demo',select:n=>{chosen=n},get:async(_,p)=>{
  calls++;return {revision:3,nodes:p.nodeId===node.nodeId?[type]:[{nodeId:'symbol:method',label:'.pick()',line:4,details:{entityKind:'method'}}]};
}});
assert.equal(calls,0);const panel=box.children[0];panel.open=true;await panel.events.toggle();await new Promise(r=>setTimeout(r,0));
assert.equal(calls,1);assert.match(collect(box),/Switches/);assert.doesNotMatch(collect(box),/pick/);
const children=panel.children.find(e=>e.tag==='div').children.find(e=>e.tag==='details');children.open=true;await children.events.toggle();await new Promise(r=>setTimeout(r,0));
assert.match(collect(children),/Switches.pick\(\)/);children.children.find(e=>e.tag==='div').children[0].onclick();assert.equal(chosen.label,'Switches.pick()');
const partialBox=new Element('div');symbolTree(node,partialBox,{projectId:'demo',select:()=>{},get:async()=>({revision:3,owners:{'symbol:elsewhere':'Part.Service'},nodes:[{nodeId:'symbol:part',label:'.save()',details:{entityKind:'method',ownerNodeId:'symbol:elsewhere'}}]})});
partialBox.children[0].open=true;await partialBox.children[0].events.toggle();await new Promise(r=>setTimeout(r,0));
assert.match(collect(partialBox),/Part.Service.save\(\)/);
let refreshed=0;const section=new Element('section'),note=new Element('p');
await roleJob(node,section,note,{projectId:'demo',refresh:()=>refreshed++,get:async()=>({status:'SUCCESS'})});
assert.equal(refreshed,0); // A successful old job must not loop refreshing a stale explanation.
let body;globalThis.fetch=async(_,p)=>{body=JSON.parse(p.body);return {ok:true,json:async()=>({status:'QUEUED'})};};
await section.children[0].onclick();assert.equal(body.nodeId,node.nodeId);assert.equal(refreshed,1);
const detached=new Element('section');detached.isConnected=false;await roleJob(node,detached,note,{projectId:'demo',get:()=>{throw Error('detached fetch')},refresh:()=>{}});
console.log('PASS lazy symbol hierarchy, qualified names, selection, role request and stale-job refresh guard');
