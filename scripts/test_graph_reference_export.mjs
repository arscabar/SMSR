import assert from 'node:assert/strict';
import fs from 'node:fs';
const folder=process.argv[2];
assert(folder,'export folder required');
for(const project of ['SMSR','AO3.5_Main']){
 const data=JSON.parse(fs.readFileSync(`${folder}/${project}.json`,'utf8'));
 assert.equal(data.truncated,false);
 const ids=new Set(data.nodes.map(n=>n.nodeId)),relations={};
 for(const node of data.nodes)if(node.details?.ownerNodeId)assert(ids.has(node.details.ownerNodeId));
 for(const edge of data.edges){
  assert(ids.has(edge.sourceId)&&ids.has(edge.targetId));
  assert(edge.evidence?.sourceHash&&edge.evidence?.analyzer);
  relations[edge.relation]=(relations[edge.relation]||0)+1;
 }
 assert(relations.CODE_BEHIND>0&&relations.BINDS_TO>0);
 const propertyBindings=data.edges.filter(e=>['BINDS_TO','COMMAND_BINDS_TO'].includes(e.relation)&&data.nodes.some(n=>n.nodeId===e.targetId&&n.details?.entityKind==='property'));
 if(project==='AO3.5_Main')assert(relations.VIEW_MODEL>0&&propertyBindings.length>0,'real property binding required');
 console.log(JSON.stringify({project,revision:data.revision,nodes:data.nodes.length,edges:data.edges.length,propertyBindings:propertyBindings.length,relations,truncated:false,missingEvidence:0}));
}
