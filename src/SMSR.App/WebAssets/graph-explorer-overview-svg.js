const ns='http://www.w3.org/2000/svg';
function svg(tag,attrs={},label){const n=document.createElementNS(ns,tag);for(const [k,v] of Object.entries(attrs))n.setAttribute(k,v);if(label!==undefined)n.textContent=label;return n;}
export function groupMap(data,open){
  const canvas=svg('svg',{viewBox:`0 0 720 ${Math.max(140,Math.ceil(data.groups.length/3)*100+30)}`,role:'group','aria-label':'구조 그룹 사이 실제 방향 연결'});
  const marker=svg('marker',{id:'group-arrow',viewBox:'0 0 10 10',refX:9,refY:5,markerWidth:6,markerHeight:6,orient:'auto-start-reverse'});
  marker.append(svg('path',{d:'M0 0L10 5L0 10Z',fill:'#62adff'}));const defs=svg('defs');defs.append(marker);canvas.append(defs);
  const positions=new Map(data.groups.map((g,i)=>[g.id,{x:10+(i%3)*240,y:15+Math.floor(i/3)*100}]));
  for(const link of data.links){const a=positions.get(link.sourceGroup),b=positions.get(link.targetGroup);if(!a||!b)continue;
    const line=svg('line',{x1:a.x+110,y1:a.y+35,x2:b.x+110,y2:b.y+35,'marker-end':'url(#group-arrow)'});
    line.append(svg('title',{},`${link.sourceGroup} → ${link.targetGroup}: ${link.relation} ${link.count}개 · ${link.confidence}`));canvas.append(line);}
  for(const group of data.groups){const p=positions.get(group.id),g=svg('g',{role:'button',tabindex:0,'aria-label':`${group.name}, ${group.nodeCount}개, 펼치기`});
    g.append(svg('rect',{x:p.x,y:p.y,width:220,height:72,rx:10}),svg('text',{x:p.x+12,y:p.y+25},group.name.length>25?group.name.slice(0,24)+'…':group.name),svg('text',{x:p.x+12,y:p.y+50},`${group.nodeCount}개 · 응집도 ${(group.cohesion*100).toFixed(1)}%`));
    g.append(svg('title',{},group.name));g.addEventListener('click',()=>open(group.id));g.addEventListener('keydown',e=>{if(['Enter',' '].includes(e.key)){e.preventDefault();open(group.id)}});canvas.append(g);}
  return canvas;
}
