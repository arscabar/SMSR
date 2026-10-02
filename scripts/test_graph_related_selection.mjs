import assert from 'node:assert/strict';
class Element{
  constructor(tag){this.tag=tag;this.children=[];this.textContent='';}
  append(...items){this.children.push(...items);}
}
globalThis.document={createElement:tag=>new Element(tag)};
const {relatedItems}=await import('../src/SMSR.App/WebAssets/graph-explorer-related.js');
const a={nodeId:'symbol:a',kind:'symbol',label:'A',ownerPath:'same.cs'};
const b={nodeId:'symbol:b',kind:'symbol',label:'B',ownerPath:'same.cs'};
const c={nodeId:'symbol:c',kind:'symbol',label:'C',ownerPath:'other.cs'};
const edge={relation:'CALLS',confidence:'EXTRACTED',ownerPath:'same.cs',sourceLine:3};
async function chosen(node,from,to){
  const box=new Element('div');let selected;
  await relatedItems(node,box,{projectId:'test',get:async()=>({links:[{from,to,edge}]}),sourceLink:()=>null,select:value=>{selected=value;}});
  box.children[0].children.find(e=>e.className==='related-item').children[0].onclick();
  return selected;
}
assert.equal(await chosen(b,a,b),a,'same-file incoming must select source, not selected target');
assert.equal(await chosen(a,a,b),b,'same-file outgoing must select target');
assert.equal(await chosen(b,c,b),c,'cross-file incoming must select source');
assert.equal(await chosen(b,b,c),c,'cross-file outgoing must select target');
const file={nodeId:'file:same.cs',kind:'code',ownerPath:'same.cs'};
assert.equal(await chosen(file,c,b),c,'file-level incoming owner projection stays supported');
assert.equal(await chosen(file,a,c),c,'file-level outgoing owner projection stays supported');
console.log('PASS related selection uses node ID before file ownership');
