import {heightOf} from './graph-explorer-svg.js';

export function edgePoints(a,b,offset=0){
  const forward=a.x<b.x;
  const points={x1:a.x+(forward?200:0),y1:a.y+heightOf(a.node)/2,
    x2:b.x+(forward?0:200),y2:b.y+heightOf(b.node)/2};
  const dx=points.x2-points.x1,dy=points.y2-points.y1,length=Math.hypot(dx,dy)||1;
  for(const index of [1,2]){points['x'+index]-=dy*offset/length;points['y'+index]+=dx*offset/length;}
  return points;
}
export function edgeEntries(layout){
  const entries=[],pairs=new Map();
  for(const edges of layout.links){
    const edge=edges[0],a=layout.coords.get(edge.sourceId),b=layout.coords.get(edge.targetId);
    if(!a||!b||a===b)continue;
    const entry={edges,a,b,offset:0},key=JSON.stringify([edge.sourceId,edge.targetId].sort());
    if(!pairs.has(key))pairs.set(key,[]);pairs.get(key).push(entry);entries.push(entry);
  }
  for(const pair of pairs.values())pair.forEach((entry,index)=>{
    const edge=entry.edges[0];
    const spacing=Math.min(24,(Math.min(heightOf(entry.a.node),heightOf(entry.b.node))-12)/Math.max(1,pair.length-1));
    entry.offset=(index-(pair.length-1)/2)*spacing*(edge.sourceId<edge.targetId?1:-1);
  });
  return entries;
}
export function nearestEdge(entries,point){
  let best,distance=Infinity;
  for(const entry of entries){
    const {x1,y1,x2,y2}=edgePoints(entry.a,entry.b,entry.offset),dx=x2-x1,dy=y2-y1;
    const t=Math.max(0,Math.min(1,((point.x-x1)*dx+(point.y-y1)*dy)/(dx*dx+dy*dy||1)));
    const value=(point.x-x1-t*dx)**2+(point.y-y1-t*dy)**2;
    if(value<distance){best=entry;distance=value;}
  }
  return best;
}
