import {positions} from './graph-explorer-network.js';
import {svgElement as make,svgNode,heightOf} from './graph-explorer-svg.js';
import {svgEdges} from './graph-explorer-edges.js';
import {bounded,dragNode} from './graph-explorer-drag.js';
import {showEvidenceList} from './graph-explorer-evidence.js?v=3';
import {viewport} from './graph-explorer-viewport.js';

const placements=new WeakMap();
export function clearRelationGraph(graph) {
  placements.get(graph)?.dispose();placements.delete(graph);
  const reset=document.querySelector('#relation-reset');if(reset){reset.onclick=null;reset.disabled=true}
}

export function drawRelations(data,{graph,details,sourceLink,select,detail}) {
  const root=document.querySelector('#explorer');
  const view=new URLSearchParams(globalThis.location?.search||'').get('view')||'find';
  const key=JSON.stringify([root?.dataset.project,data.revision,data.node.nodeId,view,data.direction,data.relation]);
  let state=placements.get(graph);state?.dispose();
  if(state?.key!==key){state={key,values:new Map(),dispose:()=>{}};placements.set(graph,state)}
  const reset=document.querySelector('#relation-reset');
  reset.disabled=state.values.size===0;
  reset.onclick=()=>{state.values.clear();drawRelations(data,{graph,details,sourceLink,select,detail})};
  graph.replaceChildren();document.querySelector('#graph-tools').hidden=false;
  const layout=positions(data,heightOf),canvas={width:layout.width+220,height:layout.height+120};
  const svg=make('svg',{role:'group',viewBox:`0 0 ${canvas.width} ${canvas.height}`,'aria-label':`${data.node.label}의 관계망`});
  Object.assign(svg.style,{width:'100%',height:'100%',minWidth:'0'});
  svg.setAttribute('preserveAspectRatio','xMidYMid meet');
  for(const [id,position] of layout.coords){const saved=state.values.get(id);if(saved)Object.assign(position,bounded(saved,{...canvas,nodeHeight:heightOf(position.node)}))}
  const defs=make('defs'),marker=make('marker',{id:'relation-arrow',markerWidth:8,markerHeight:8,refX:7,refY:4,orient:'auto'});
  marker.append(make('path',{d:'M0 0 L8 4 L0 8 Z',fill:'#88a7ca'}));defs.append(marker);svg.append(defs);
  const updateEdges=svgEdges(svg,layout,(edges,a,b)=>showEvidenceList(details,edges,a,b,sourceLink));
  const cleanup=[];
  for(const position of layout.coords.values()) {
    const {node,x,y}=position,group=svgNode(svg,node,x,y,node.nodeId===data.node.nodeId,()=>node.nodeId===data.node.nodeId?detail(node):select(node));
    cleanup.push(dragNode(group,svg,{x,y},{...canvas,nodeHeight:heightOf(node)},value=>{
      Object.assign(position,value);group.setAttribute('transform',`translate(${value.x-x} ${value.y-y})`);updateEdges();
    },value=>{state.values.set(node.nodeId,value);reset.disabled=false}));
  }
  graph.append(svg);
  cleanup.push(viewport(graph,svg,canvas));
  state.dispose=()=>cleanup.forEach(dispose=>dispose());
}
