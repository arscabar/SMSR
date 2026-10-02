import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';
const nodes=Array.from({length:55},(_,i)=>({nodeId:`n${i}`,label:`한글 ${i}`,sourcePath:`src/${i}.cs`}));
const data={nodes,edges:[{sourceId:'n0',targetId:'n1',relation:'CALLS'}],hyperedges:[{members:[{nodeId:'n0'}]}],overview:{groups:[{id:1,name:'그룹',memberIds:['n0','n1']}]}};
class Element{
 constructor(tag){this.tag=tag;this.children=[];this.events={};this.value='';this.attributes={};}
 append(...children){this.children.push(...children);}
 before(child){this.beforeElement=child;}
 replaceChildren(...children){this.children=children;}
 setAttribute(key,value){this.attributes[key]=value;}
 addEventListener(key,handler){this.events[key]=handler;}
}
function check(input,script){
 const elements=new Map(['data','query','previous','next','nodes','map','limit','detail','position'].map(id=>[id,new Element(id)]));
 elements.get('data').textContent=JSON.stringify(input);
 vm.runInNewContext(script,{document:{getElementById:id=>elements.get(id),createElement:tag=>new Element(tag),createElementNS:(_,tag)=>new Element(tag)}});
 return elements;
}
const script=fs.readFileSync('src/SMSR.App/WebAssets/graph-export.js','utf8');
const el=check(data,script);
assert.equal(el.get('nodes').children.length,50);
el.get('next').onclick();assert.equal(el.get('nodes').children.length,5);
el.get('previous').onclick();assert.equal(el.get('nodes').children.length,50);
const groups=el.get('query').beforeElement;groups.value='1';groups.onchange();assert.equal(el.get('nodes').children.length,2);
el.get('query').value='한글 1';el.get('query').oninput();assert.equal(el.get('nodes').children.length,1);
el.get('nodes').children[0].onclick();assert.equal(JSON.parse(el.get('detail').textContent).node.nodeId,'n1');
const origin=el.get('map').children.find(n=>n.attributes['aria-label']==='한글 0');
origin.events.keydown({key:'Enter',preventDefault(){}});assert.equal(JSON.parse(el.get('detail').textContent).node.nodeId,'n0');
assert.equal(JSON.parse(el.get('detail').textContent).hyperedges.length,1);
if(process.argv[2]){
 const html=fs.readFileSync(process.argv[2],'utf8');assert(html.includes("connect-src 'none'"));assert(!/<script[^>]+src=/i.test(html));
 const exported=JSON.parse(html.match(/id="data">([\s\S]*?)<\/script>/)[1]);
 const result=check(exported,script);assert(result.get('nodes').children.length>0);assert(result.get('detail').textContent);
}
console.log('PASS offline export paging, group, search, keyboard, details, CSP and downloaded data');
