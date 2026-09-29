const supported = /\.(cs|py|js|jsx|ts|tsx|java|go|rs|c|h|cpp|hpp|rb|php|kt|swift|scala|lua)$/i;
export const supportsFlow = node => node.kind === 'code' && supported.test(node.sourcePath || '');

async function analysis(projectId, path, action) {
  const response = await fetch('/api/graph/' + action,{method:'POST',headers:{'Content-Type':'application/json'},
    body:JSON.stringify({projectId,input:path})});
  const data = await response.json().catch(()=>({}));
  if (!response.ok) throw new Error(data.error || `분석 요청 실패 (${response.status})`);
  return data;
}
function label(value) { return String(value).replace(/_statement$/,'').replaceAll('_',' '); }
function render(report, graph, edges) {
  graph.replaceChildren(); edges.replaceChildren();
  const result=report.result || {}, all=result.statements || [], first=all[0];
  const statements=first ? all.filter(item=>item.scope===first.scope).slice(0,9) : [];
  const visible=new Set(statements.map(item=>item.id));
  const links=(result.edges || []).filter(item=>visible.has(item.source)&&visible.has(item.target)).slice(0,24);
  if (!statements.length) {graph.textContent='이 파일에서 표시할 함수 내부 분석 결과가 없습니다.';return}
  const ns='http://www.w3.org/2000/svg',svg=document.createElementNS(ns,'svg');
  svg.setAttribute('viewBox',`0 0 700 ${Math.max(430,statements.length*84)}`);
  svg.setAttribute('aria-label','함수 내부의 보수적 데이터·제어 의존 관계');
  const defs=document.createElementNS(ns,'defs'),marker=document.createElementNS(ns,'marker'),arrow=document.createElementNS(ns,'path');
  for(const [key,value] of Object.entries({id:'flow-arrow',markerWidth:8,markerHeight:8,refX:7,refY:4,orient:'auto'})) marker.setAttribute(key,value);
  arrow.setAttribute('d','M0 0 L8 4 L0 8 Z');arrow.setAttribute('fill','#88a7ca');marker.append(arrow);defs.append(marker);svg.append(defs);
  const locations=new Map(statements.map((item,index)=>[item.id,{x:index%2?400:35,y:20+Math.floor(index/2)*84}]));
  for(const edge of links){
    const from=locations.get(edge.source),to=locations.get(edge.target);
    const line=document.createElementNS(ns,'line');
    for(const [key,value] of Object.entries({x1:from.x+110,y1:from.y+25,x2:to.x+110,y2:to.y+25})) line.setAttribute(key,value);
    line.setAttribute('marker-end','url(#flow-arrow)');
    svg.append(line);
    const source=statements.find(item=>item.id===edge.source),target=statements.find(item=>item.id===edge.target);
    const item=document.createElement('li');
    item.textContent=`${source.line}줄 → ${target.line}줄 · ${edge.relation}${edge.variable?' · '+edge.variable:''}`;
    edges.append(item);
  }
  for(const item of statements){
    const {x,y}=locations.get(item.id),rect=document.createElementNS(ns,'rect'),text=document.createElementNS(ns,'text');
    for(const [key,value] of Object.entries({x,y,width:220,height:52,rx:9})) rect.setAttribute(key,value);
    text.setAttribute('x',x+10);text.setAttribute('y',y+29);text.textContent=`${item.line}줄 · ${label(item.kind).slice(0,23)}`;
    svg.append(rect,text);
  }
  graph.append(svg);
  if(!links.length){const item=document.createElement('li');item.textContent='표시 범위에 저장된 의존 관계가 없습니다.';edges.append(item)}
}
export async function showFlow(projectId,node,graph,edges,details,message) {
  graph.replaceChildren();edges.replaceChildren();
  const note=document.querySelector('#graph-note');
  note.textContent='첫 함수의 문장 최대 9개에 대한 보수적 의존 관계입니다. 실행 순서나 전체 프로그램 흐름을 뜻하지 않습니다.';
  if(!supportsFlow(node)) {
    graph.textContent='지원되는 코드 파일을 선택하세요. 문서·이미지는 연결 보기에서 확인할 수 있습니다.';return;
  }
  const path=node.sourcePath;
  const button=document.createElement('button');button.type='button';button.className='action';button.textContent='분석·저장';
  const run=async()=>{
    button.disabled=true;message('코드 분석 중…',true);
    try {const data=await analysis(projectId,path,'analyze');render(data,graph,edges);message('분석 결과를 저장했습니다.');}
    catch(error){message(error.message)}finally{button.disabled=false}
  };
  button.addEventListener('click',run);
  try {
    const data=await analysis(projectId,path,'analysis');
    if(data.stale){graph.textContent='저장된 분석이 현재 코드와 다릅니다. 다시 분석하세요.';details.append(button)}
    else {render(data.report,graph,edges);details.append(button)}
  } catch(error) {
    graph.textContent=error.message.includes('저장된 분석')?'저장된 분석이 없습니다. 분석·저장을 누르세요.':error.message;
    details.append(button);
  }
}
