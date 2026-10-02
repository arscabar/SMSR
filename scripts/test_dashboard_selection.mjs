import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import vm from 'node:vm';
const source=await readFile(new URL('../src/SMSR.App/Mvp/DashboardLiveUpdates.cs',import.meta.url),'utf8');
const code=source.slice(source.indexOf('(() => {'),source.lastIndexOf('</script>')).replaceAll('{{project}}','SMSR').replaceAll('{{workflow}}','fixture');
const handlers=new Map(),storage=new Map();
const panel={scrollTop:480,scrollLeft:75,dataset:{}};let replacement;
const location={href:'http://localhost/dashboard?projectId=SMSR&workflowId=fixture',origin:'http://localhost',assign:()=>assert.fail('unexpected full navigation'),reload:()=>assert.fail('unexpected reload')};
const window={scrollX:10,scrollY:20,scrollTo:(x,y)=>{assert.equal(x,10);assert.equal(y,20);},addEventListener:(key,fn)=>handlers.set(key,fn)};
const document={getElementById:id=>['flow','graph','details','live-connection'].includes(id)?panel:null,querySelector:s=>s==='main'?{replaceWith:()=>{panel.scrollTop=0;panel.scrollLeft=0;replacement=true;}}:null,
  querySelectorAll:()=>[],addEventListener:(key,fn)=>handlers.set(key,fn)};
const context={document,window,location,URL,Map,Date,Number,String,Object,JSON,AbortController,
  history:{pushState:(_,__,url)=>{location.href=url.href;}},sessionStorage:{getItem:k=>storage.get(k),setItem:(k,v)=>storage.set(k,v)},
  EventSource:class{addEventListener(){}close(){}},DOMParser:class{parseFromString(){return{querySelector:()=>({}),getElementById:()=>null};}},
  fetch:async()=>({ok:true,text:async()=>''}),setInterval:()=>{},setTimeout:()=>0,clearTimeout:()=>{}};
vm.runInNewContext(code,context);
const select=id=>({button:0,target:{closest:s=>s==='.flow-svg a'?{getAttribute:()=>'/dashboard?projectId=SMSR&workflowId=fixture&selectedNodeId='+id}:null},preventDefault(){this.prevented=true;}});
const first=select('one');handlers.get('click')(first);
await new Promise(resolve=>setTimeout(resolve,10));
assert(first.prevented);assert(replacement);assert.equal(panel.scrollTop,480);assert.equal(panel.scrollLeft,75);
assert(location.href.includes('selectedNodeId=one'));
const modified=select('two');modified.ctrlKey=true;handlers.get('click')(modified);assert(!modified.prevented);
handlers.get('popstate')();await new Promise(resolve=>setTimeout(resolve,10));assert.equal(panel.scrollTop,480);
console.log('Dashboard selection, scroll preservation, modifier navigation, history OK');
