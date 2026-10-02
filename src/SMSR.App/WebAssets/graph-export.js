(() => {
 const data=JSON.parse(document.getElementById('data').textContent),byId=new Map(data.nodes.map(n=>[n.nodeId,n]));
 const el=id=>document.getElementById(id);let page=0,items=data.nodes;
 const groups=document.createElement('select');groups.setAttribute('aria-label','그룹 선택');
 for(const [value,label]of [['','전체'],...(data.overview?.groups||[]).map(g=>[String(g.id),g.name])]){const option=document.createElement('option');option.value=value;option.textContent=label;groups.append(option);}
 el('query').before(groups);if(!data.overview)groups.title='현재 리비전의 저장된 그룹 분석이 없습니다.';
 const svg=(tag,attributes)=>{const node=document.createElementNS('http://www.w3.org/2000/svg',tag);for(const [k,v]of Object.entries(attributes))node.setAttribute(k,v);return node;};
 function select(id){
  const node=byId.get(id),edges=data.edges.filter(e=>e.sourceId===id||e.targetId===id),neighbors=[...new Set(edges.flatMap(e=>[e.sourceId,e.targetId]))].filter(n=>n!==id).slice(0,12);
  el('detail').textContent=JSON.stringify({node,edges:edges.slice(0,100),hyperedges:data.hyperedges.filter(h=>h.members.some(m=>m.nodeId===id))},null,2);
  el('limit').textContent=`직접 연결 ${edges.length}개 · 화면은 이웃 최대 12개, 상세 관계 최대 100개 · 정적 관계이며 실행 순서 아님`;
  const map=el('map');map.replaceChildren();map.setAttribute('viewBox','0 0 1000 420');
  const points=new Map([[id,[500,210]]]);neighbors.forEach((n,i)=>{const angle=2*Math.PI*i/Math.max(1,neighbors.length);points.set(n,[500+370*Math.cos(angle),210+165*Math.sin(angle)]);});
  edges.forEach(e=>{const a=points.get(e.sourceId),b=points.get(e.targetId);if(a&&b)map.append(svg('line',{x1:a[0],y1:a[1],x2:b[0],y2:b[1],stroke:'#789', 'stroke-width':2}));});
  for(const [key,[x,y]]of points){const group=svg('g',{tabindex:0,role:'button','aria-label':byId.get(key)?.label||key});group.append(svg('rect',{x:x-75,y:y-20,width:150,height:40,rx:8,fill:key===id?'#24659c':'#244537'}));const text=svg('text',{x,y:y+4,'text-anchor':'middle',fill:'white','font-size':12});text.textContent=(byId.get(key)?.label||key).slice(0,24);group.append(text);group.addEventListener('click',()=>select(key));group.addEventListener('keydown',e=>{if(e.key==='Enter'||e.key===' '){e.preventDefault();select(key);}});map.append(group);}
 }
 function render(){el('nodes').replaceChildren(...items.slice(page*50,page*50+50).map(n=>{const button=document.createElement('button');button.textContent=n.label+' · '+n.sourcePath;button.onclick=()=>select(n.nodeId);return button;}));el('position').textContent=`${page+1}/${Math.max(1,Math.ceil(items.length/50))} · ${items.length}개`;el('previous').disabled=!page;el('next').disabled=(page+1)*50>=items.length;}
 function filter(){const q=el('query').value.toLocaleLowerCase(),group=data.overview?.groups.find(g=>String(g.id)===groups.value),ids=group?new Set(group.memberIds):null;items=data.nodes.filter(n=>(!ids||ids.has(n.nodeId))&&(n.label+' '+n.sourcePath).toLocaleLowerCase().includes(q));page=0;render();}
 el('query').oninput=filter;groups.onchange=filter;
 el('previous').onclick=()=>{page--;render();};el('next').onclick=()=>{page++;render();};render();if(items.length)select(items[0].nodeId);
})();
