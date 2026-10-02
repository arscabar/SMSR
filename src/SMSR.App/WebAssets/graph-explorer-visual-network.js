import {visualData} from './graph-explorer-visual-data.js';
import {hyperRegions} from './graph-explorer-visual-regions.js';
// Original Graphify forceAtlas2Based settings and golden-angle seeding; no custom physics engine.
export const physics={enabled:true,solver:'forceAtlas2Based',forceAtlas2Based:{gravitationalConstant:-60,
  centralGravity:.005,springLength:120,springConstant:.08,damping:.4,avoidOverlap:.8},stabilization:{iterations:200,fit:true}};
export function visualNetwork(container,data,{node,edge,open}){
  const raw=visualData(data),nodes=new vis.DataSet(raw.nodes),edges=new vis.DataSet(raw.edges);
  const network=new vis.Network(container,{nodes,edges},{physics,
    interaction:{hover:true,tooltipDelay:100,hideEdgesOnDrag:true,navigationButtons:false,keyboard:{enabled:true,bindToWindow:false}},
    nodes:{shape:'dot',borderWidth:1.5},edges:{smooth:{type:'continuous',roundness:.2},selectionWidth:3}});
  network.on('stabilizationIterationsDone',()=>{network.setOptions({physics:{enabled:false}});network.fit({animation:false});container.dataset.stable='true';});
  network.on('click',p=>{if(p.nodes.length)node(p.nodes[0]);else if(p.edges.length)edge(p.edges[0]);});
  network.on('doubleClick',p=>{if(p.nodes.length)open(p.nodes[0]);});
  hyperRegions(network,nodes,data.hyperedges);
  const fit=()=>{network.setSize('100%','100%');network.fit({animation:false});};
  const observer=new ResizeObserver(fit);observer.observe(container);
  const control={
    focus(id){if(nodes.get(id)?.hidden)return;network.focus(id,{scale:1.4,animation:!matchMedia('(prefers-reduced-motion:reduce)').matches});network.selectNodes([id]);node(id);},
    fit,zoom(factor){network.moveTo({scale:Math.max(.05,Math.min(4,network.getScale()*factor))});},
    hide(hidden){nodes.update(raw.nodes.map(n=>({id:n.id,hidden:hidden.has(n.group)})));},
    reset(){delete container.dataset.stable;nodes.update(raw.nodes.map(n=>({id:n.id,x:n.x,y:n.y})));network.setOptions({physics});network.stabilize(200);},
    mark(id,status){const n=raw.nodes.find(n=>n.id===id);if(n)nodes.update({id,borderWidth:3,color:{...n.color,border:status==='USEFUL'?'#22c55e':'#f59e0b'}});},
    destroy(){observer.disconnect();network.destroy();delete container.dataset.stable;}
  };return control;
}
