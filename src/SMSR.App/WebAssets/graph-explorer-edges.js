import {svgElement as make,button} from './graph-explorer-svg.js';
import {relationName} from './graph-explorer-labels.js';
import {edgePoints,edgeEntries,nearestEdge} from './graph-explorer-edge-layout.js';
import {edgeStyle} from './graph-explorer-edge-style.js';
import {evidenceName,resolutionName} from './graph-explorer-evidence.js';
export {edgePoints,nearestEdge} from './graph-explorer-edge-layout.js';

export function svgEdges(svg,layout,show) {
  const entries=edgeEntries(layout);
  for(const entry of entries) {
    const {edges,a,b}=entry,edge=edges[0],style=edgeStyle(edge);
    const line=make('line',{'marker-end':'url(#relation-arrow)'});
    Object.assign(line.style,{stroke:style.stroke,strokeDasharray:style.strokeDasharray,animation:'none'});svg.append(line);
    const hit=line.cloneNode();hit.removeAttribute('marker-end');
    Object.assign(hit.style,{stroke:'transparent',strokeWidth:'16',strokeDasharray:'none',animation:'none',pointerEvents:'stroke'});
    const label=`${relationName(edge.relation)} · ${evidenceName(edge)} · ${edge.resolution?resolutionName(edge):'대상 해소 정보 없음'} · ${a.node.label} → ${b.node.label} · 근거 ${edges.length}개`;
    const choose=event=>{
      let picked=entry;
      if(event?.type==='click'&&event.detail>0){
        const matrix=svg.getScreenCTM();
        if(matrix){const point=svg.createSVGPoint();point.x=event.clientX;point.y=event.clientY;
          picked=nearestEdge(entries,point.matrixTransform(matrix.inverse()))||entry}
      }
      show(picked.edges,picked.a.node,picked.b.node);
    };
    button(hit,label,choose);
    const hint=make('title');hint.textContent=label;hit.append(hint);svg.append(hit);
    const caption=make('text',{'text-anchor':'middle',class:'edge-label'});
    caption.textContent=`${relationName(edge.relation)} · ${style.label}`;
    Object.assign(caption.style,{fontSize:'10px',paintOrder:'stroke',stroke:'var(--surface)',strokeWidth:'3px',cursor:'pointer'});
    button(caption,label,()=>show(edges,a.node,b.node));svg.append(caption);
    Object.assign(entry,{line,hit,caption});
  }
  const update=()=>{for(const {line,hit,caption,a,b,offset} of entries){
    const points=edgePoints(a,b,offset);
    for(const [key,value] of Object.entries(points)){line.setAttribute(key,value);hit.setAttribute(key,value);}
    caption.setAttribute('x',(points.x1+points.x2)/2);caption.setAttribute('y',(points.y1+points.y2)/2-4);
  }};
  update();return update;
}
