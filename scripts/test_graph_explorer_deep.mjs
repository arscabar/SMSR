import assert from 'node:assert/strict';
class Node {
  constructor(tag){this.tag=tag;this.children=[];this.textContent='';}
  append(...items){this.children.push(...items)}
  replaceChildren(...items){this.children=items}
}
globalThis.document={createElement:tag=>new Node(tag)};
const {deepEvidence,deepStatus}=await import('../src/SMSR.App/WebAssets/graph-explorer-deep.js');
const all=node=>node.textContent+' '+node.children.map(all).join(' ');
const panel=new Node('div');
assert.equal(deepEvidence(panel,{}),false);
assert.equal(deepEvidence(panel,{analysis:[{analyzer:'csharp',analysisVersion:3,analysisKey:'<img onerror=x>',profile:'static',binding:'unique',dispatch:'static',inputHash:'A'.repeat(64),settingsHash:'B'.repeat(64)}]}),true);
assert.ok(all(panel).includes('C# · 분석 버전 3'));
assert.ok(all(panel).includes('<img onerror=x>'));
assert.ok(!all(panel).includes('A'.repeat(13)));
assert.ok(!panel.children.some(n=>n.tag==='img'));
await deepStatus(panel,async()=>[], 'p');
assert.ok(all(panel).includes('저장된 심층 결과 없음'));
await deepStatus(panel,async()=>[{status:'APPLIED'},{status:'APPLIED'},{status:'INPUT_CHANGED_OR_REMOVED'}], 'p');
assert.ok(all(panel).includes('관계에 통합됨 · 2건'));
assert.ok(all(panel).includes('원문 변경·제거 · 1건'));
await deepStatus(panel,async()=>{throw new Error('offline')}, 'p');
assert.ok(all(panel).includes('확인 실패: offline'));
console.log('Deep provenance DOM, text safety, state aggregation and failures passed');
