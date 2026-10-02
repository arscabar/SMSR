import assert from 'node:assert/strict';
import {network,positions} from '../src/SMSR.App/WebAssets/graph-explorer-network.js';
const nodes=['a','b','c'].map(nodeId=>({nodeId,label:nodeId}));
const edge=(sourceId,targetId,sourceLine)=>({sourceId,targetId,sourceLine,relation:'CALLS',ownerPath:'a.cs'});
const data={node:nodes[1],incoming:[{node:nodes[0],edge:edge('a','b',1)},{node:nodes[0],edge:edge('a','b',2)}],
  outgoing:[{node:nodes[0],edge:edge('b','a',3)}],nodes,edges:[edge('b','c',4)]};
const result=network(data);
assert.equal(result.nodes.length,3);assert.equal(result.links.length,3);
assert.equal(result.links[0].length,2);assert.equal(positions(data,()=>50).coords.size,3);
assert.equal(network({...data,edges:[...data.edges,...data.edges]}).links.length,3);
assert.equal(positions({node:nodes[1],incoming:[{node:nodes[0],edge:edge('a','b',1)}]},()=>50).width,660);
const confirmed={...edge('a','b',1),confidence:'EXTRACTED',resolution:'RESOLVED'};
const inferred={...confirmed,confidence:'INFERRED',sourceLine:2};
const candidate={...confirmed,resolution:'INFERRED',sourceLine:3};
assert.equal(network({node:nodes[0],nodes,edges:[confirmed,inferred,candidate]}).links.length,3,'confidence and target resolution must not be folded into first-edge certainty');
console.log('Unique nodes/repeated call-site evidence/cycles/expanded layout OK');
