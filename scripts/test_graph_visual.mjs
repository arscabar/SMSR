import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {visualData} from '../src/SMSR.App/WebAssets/graph-explorer-visual-data.js';
import {visualNetwork,physics} from '../src/SMSR.App/WebAssets/graph-explorer-visual-network.js';
import {convexHull} from '../src/SMSR.App/WebAssets/graph-explorer-visual-regions.js';
const data={mode:'nodes',nodes:[{id:'a',label:'<img onerror=bad>',groupId:1,groupName:'A',degree:2,members:1},{id:'b',label:'B',groupId:2,groupName:'B',degree:1,members:1}],
  edges:[{id:'e',sourceId:'b',targetId:'a',relation:'CALLS',confidence:'INFERRED',count:1}],hyperedges:[]};
const raw=visualData(data);assert.equal(raw.nodes[0].title,'<img onerror=bad>\nA');assert.equal(raw.edges[0].from,'b');assert(raw.edges[0].dashes);
assert.equal(physics.solver,'forceAtlas2Based');assert.equal(physics.stabilization.iterations,200);assert.equal(physics.forceAtlas2Based.avoidOverlap,.8);
let instance,disconnected=false;
globalThis.vis={DataSet:class{constructor(rows){this.rows=rows;}get(id){return this.rows.find(n=>n.id===id)}update(rows){for(const r of rows)Object.assign(this.get(r.id),r);}},
 Network:class{constructor(el,data,opts){instance=this;this.data=data;this.opts=opts;this.events={};}once(n,fn){this.events[n]=fn}on(n,fn){this.events[n]=fn}setOptions(o){this.options=o}setSize(w,h){this.dimensions=[w,h]}fit(){this.fitted=true}moveTo(o){this.move=o}getScale(){return 1}focus(id){this.focused=id}selectNodes(ids){this.selected=ids}stabilize(n){this.iterations=n}destroy(){this.dead=true}}};
globalThis.ResizeObserver=class{observe(){}disconnect(){disconnected=true}};globalThis.matchMedia=()=>({matches:true});
const el={dataset:{}},clicked=[];const graph=visualNetwork(el,data,{node:id=>clicked.push(id),edge(){},open(){}});
instance.events.stabilizationIterationsDone();assert.equal(instance.options.physics.enabled,false);assert.equal(el.dataset.stable,'true');
graph.hide(new Set([1]));graph.focus('a');assert.equal(clicked.length,0);graph.focus('b');assert.deepEqual(clicked,['b']);
graph.zoom(2);assert.equal(instance.move.scale,2);graph.fit();assert.deepEqual(instance.dimensions,['100%','100%']);graph.reset();assert.equal(instance.iterations,200);graph.destroy();assert(disconnected&&instance.dead);
assert.equal(convexHull([{x:0,y:0},{x:2,y:2},{x:0,y:2},{x:2,y:0},{x:1,y:1}]).length,4);
const bundle=await readFile('src/SMSR.App/WebAssets/vis-network-9.1.6.min.js');assert.equal(createHash('sha384').update(bundle).digest('base64'),'Ux6phic9PEHJ38YtrijhkzyJ8yQlH8i/+buBR8s3mAZOJrP1gwyvAcIYl3GWtpX1');
const attributes=await readFile('.gitattributes','utf8');assert.match(attributes,/src\/SMSR\.App\/WebAssets\/vis-network-9\.1\.6\.min\.js -text/);
console.log('PASS original physics/SRI, direction/confidence, plain-text labels, filters/focus/reset/cleanup, convex hull');
