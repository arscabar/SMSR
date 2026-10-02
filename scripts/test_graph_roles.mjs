import assert from 'node:assert/strict';

class Element{
  constructor(tag){this.tag=tag;this.children=[];this.textContent='';}
  append(...items){this.children.push(...items);}
  replaceChildren(...items){this.children=items;}
}
const panel=new Element('details');
globalThis.document={createElement:tag=>new Element(tag),querySelector:()=>panel};
globalThis.window={};
const {roleDetails,citationLine}=await import('../src/SMSR.App/WebAssets/graph-explorer-role.js');
assert.equal(citationLine({line:12,quote:'header\nbody\nreturn 1'},'return 1'),14);
assert.equal(citationLine({line:12,quote:'header\nbody'},'absent'),null);
assert.equal(citationLine({line:12,quote:'a\r\nb\r\nc'},'c'),14);
const {relatedItems}=await import('../src/SMSR.App/WebAssets/graph-explorer-related.js');
const node={nodeId:'file:main.py',sourcePath:'main.py',ownerPath:'main.py',kind:'code'};
const collect=e=>[e.textContent,...e.children.map(collect)].join(' ');
const base={projectId:'demo',sourceLink:()=>null};
for(const status of ['MISSING','STALE']){
  const box=new Element('div');await roleDetails(node,box,{...base,get:async()=>({status,reason:'분석 필요',report:{claims:[{text:'이전 설명'}]}})});
  assert.match(collect(box),/분석 필요/);assert.doesNotMatch(collect(box),/이전 설명/);
}
const box=new Element('div');
await roleDetails(node,box,{...base,get:async()=>({status:'CURRENT',report:{model:'host',createdAt:'now',claims:[{kind:'ROLE',text:'ROLE: 실제 역할 <script>',confidence:'INFERRED',evidenceIds:['e0'],supportingQuote:'return 1'}],evidence:[{id:'e0',path:'main.py',line:1}]}})});
assert.match(collect(box),/실제 역할 <script>/);assert.match(collect(box),/추정/);
assert.doesNotMatch(collect(box),/ROLE:/);
assert.equal(box.children[0].children.at(-1).tag,'details');
assert.notEqual(box.children[0].children.at(-1).open,true);
const anchored=new Element('div');let replacement;
const anchor={isConnected:true,replaceWith:value=>{replacement=value;}};
await roleDetails(node,anchored,{...base,get:async()=>({status:'CURRENT',report:{model:'test',createdAt:'now',claims:[],evidence:[]}})},anchor);
assert.equal(anchored.children.length,0);assert.equal(replacement.tag,'section');
const related=new Element('div'),next={nodeId:'symbol:target',ownerPath:'target.py',label:'target'};
let chosen;
await relatedItems(node,related,{...base,select:n=>{chosen=n;},get:async()=>({links:[{from:{ownerPath:'main.py',label:'main'},to:next,edge:{relation:'CALLS',confidence:'INFERRED',ownerPath:'main.py',sourceLine:2}}]})});
assert.match(collect(related),/main → target/);assert.match(collect(related),/추정/);
related.children[0].children.find(e=>e.className==='related-item').children[0].onclick();
assert.equal(chosen,next);
const late=new Element('div');let done;
const stale=new Element('div');
await relatedItems(node,stale,{...base,select:()=>{},sourceLink:()=>new Element('a'),get:async()=>({unavailable:['main.py'],links:[{from:{label:'main'},to:next,edge:{relation:'CALLS',ownerPath:'main.py',sourceLine:2,confidence:'AMBIGUOUS'}}]})});
assert.match(collect(stale),/원문 변경 또는 근거 확인 불가/);
assert.match(collect(stale),/모호/);
assert.equal(stale.children[0].children.find(e=>e.className==='related-item').children.filter(e=>e.tag==='a').length,0);
const pending=roleDetails(node,late,{...base,get:()=>new Promise(resolve=>{done=resolve;})});
late.replaceChildren(new Element('h3'));done({status:'MISSING',reason:'old'});await pending;
assert.doesNotMatch(collect(late),/old/);
console.log('PASS role current/missing/stale, plain text, folded evidence, related navigation, late result isolation');
