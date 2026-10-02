import assert from 'node:assert/strict';
import fs from 'node:fs';
const folder=process.argv[2];
assert(folder,'export folder required');
for(const [project,revision,nodes,edges,unknown] of [
 ['SMSR',61,6930,20434,244],['AO3.5_Main',11,31399,66343,5848]]){
 const data=JSON.parse(fs.readFileSync(`${folder}/${project}.json`,'utf8'));
 assert.equal(data.revision,revision);assert.equal(data.truncated,false);
 assert.equal(data.nodes.length,nodes);assert.equal(data.edges.length,edges);
 const kinds={},ids=new Set(data.nodes.map(n=>n.nodeId));
 for(const node of data.nodes){
  if(node.kind!=='symbol')continue;
  const kind=node.details?.entityKind||'unknown';kinds[kind]=(kinds[kind]||0)+1;
  if(node.details?.ownerNodeId)assert(ids.has(node.details.ownerNodeId));
 }
 assert.equal(kinds.unknown,unknown);
 for(const edge of data.edges){
  assert(ids.has(edge.sourceId)&&ids.has(edge.targetId));
  assert(edge.evidence?.sourceHash&&edge.evidence?.analyzer);
 }
 if(project==='AO3.5_Main'){
  const window=data.nodes.find(n=>n.nodeId==='symbol:4c5035cebf1672585481efaac57051560c141e5804a999df14bec67f00bb47d4');
  assert.equal(window.details.entityKind,'property');assert.equal(window.details.endLine,25);
  assert.equal(window.details.sourceLocation,'L22');assert(window.details.ownerNodeId);
  assert(data.nodes.some(n=>n.ownerPath==='Net5/gridone.FontGlyph/IconChar.cs'&&n.details?.entityKind==='enum_member'));
 }
 console.log(JSON.stringify({project,revision,nodes,edges,kinds,truncated:false,missingEvidence:0}));
}
