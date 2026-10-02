import assert from 'node:assert/strict';
class Element {
  children=[];value='';checked=false;disabled=false;open=true;isConnected=true;
  append(...x){this.children.push(...x);} replaceChildren(...x){this.children=x;}
  add(x){this.children.push(x);} addEventListener(name,fn){this[name]=fn;}
}
const fields=new Map();
const field=id=>{if(!fields.has(id))fields.set(id,new Element());return fields.get(id);};
globalThis.document={querySelector:s=>field(s.slice(1)),createElement:()=>new Element()};
globalThis.Option=class {constructor(text,value){this.text=text;this.value=value;}};
let response,fail=false;
const settle=async()=>{for(let i=0;i<12;i++)await Promise.resolve();};
globalThis.fetch=async()=>({ok:!fail,status:409,json:async()=>response});
globalThis.setTimeout=()=>0;globalThis.clearTimeout=()=>{};
const {roleBatch}=await import('../src/SMSR.App/WebAssets/graph-explorer-role-batch.js');
const {vaultPanel,vaultNote}=await import('../src/SMSR.App/WebAssets/graph-explorer-vault.js');
const status={batch:{items:[{}],reused:1,paused:false},success:1,failed:0,running:0,pending:0,queued:0};
roleBatch({projectId:'demo',get:async()=>status});
response={revision:1,files:[{path:'a.py',status:'CURRENT'}],eligible:0,current:1,excluded:0};
await field('batch-preview').onclick(); assert.equal(field('batch-start').disabled,true);
field('batch-consent').checked=true;field('batch-consent').onchange();assert.equal(field('batch-start').disabled,false);
await field('batch-start').onclick();assert.match(field('batch-status').textContent,/완료 1\/1/);
await field('batch-preview').onclick();fail=true;await field('batch-preview').onclick();
field('batch-consent').checked=true;field('batch-consent').onchange();assert.equal(field('batch-start').disabled,true);
fail=false;let binding=null;
vaultPanel({projectId:'demo',get:async()=>({binding})});
field('vault-panel').toggle();await settle();assert.equal(field('vault-sync').disabled,true);
binding={vaultPath:'C:/temporary-vault',autoSync:true};field('vault-panel').toggle();await settle();
assert.equal(field('vault-path').value,binding.vaultPath);assert.equal(field('vault-auto').checked,true);
const box=new Element();await vaultNote({nodeId:'file:a.py'},box,{projectId:'demo',get:async()=>({uri:'javascript:bad'})});
assert.equal(box.children[0].children.length,0);
const linked=new Element();await vaultNote({nodeId:'file:a.py'},linked,{projectId:'demo',get:async()=>({uri:'obsidian://open?path=C%3A'})});
assert.match(linked.children[0].children[0].href,/^obsidian:\/\/open\?path=/);
console.log('PASS consent, current reuse, completed status, failed preview reset, vault binding, safe URI');
