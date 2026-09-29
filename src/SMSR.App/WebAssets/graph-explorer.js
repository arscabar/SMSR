import { kindName, kindClass, relationName, labelLines } from './graph-explorer-labels.js';

const root = document.querySelector('#explorer');
const projectId = root.dataset.project, workflowId = root.dataset.workflow;
const results = document.querySelector('#results'), details = document.querySelector('#details');
const graph = document.querySelector('#graph');
const status = document.querySelector('#status');
const indexPanel = document.querySelector('#index-panel'), indexForm = document.querySelector('#index-form');
let request = 0, selectedKind = '', indexed = false, page = 0, relationData = null, expanded = false;
const pageSize = 60, pages = document.querySelector('#search-pages');

function element(tag, label, className) {
  const node = document.createElement(tag);
  if (label !== undefined) node.textContent = label;
  if (className) node.className = className;
  return node;
}
function message(value, loading = false) {
  status.textContent = value;
  if (loading) window.smsrLoading?.show(status, 'searching', value);
  else window.smsrLoading?.hide(status);
}
async function get(path, params) {
  const response = await fetch(path + '?' + new URLSearchParams(params));
  if (!response.ok) throw new Error((await response.json().catch(() => ({}))).error || `요청 실패 (${response.status})`);
  return response.json();
}
function sourceLink(node) {
  if (!node.sourcePath || ['image','video','audio'].includes(node.kind)) return null;
  const link = element('a', node.kind === 'code' ? '코드 원문 열기' : '문서 원문 열기', 'source-action');
  link.href = '/graph/source?' + new URLSearchParams({projectId, path:node.sourcePath,
    line:String(Math.max(1,node.line || 1)),...(workflowId?{workflowId}:{})});
  return link;
}
function detail(node) {
  details.replaceChildren(element('h3', node.label));
  details.append(element('p', `종류: ${kindName(node.kind)}`), element('p', `위치: ${node.sourcePath || '없음'}${node.line ? ':' + node.line : ''}`));
  const link = sourceLink(node); if (link) details.append(link);
  if (!['image','video','audio'].includes(node.kind)) return;
  if (node.sourcePath.toLowerCase().endsWith('.svg')) {
    details.append(element('p','SVG는 보안을 위해 웹 미리보기를 제공하지 않습니다.','muted')); return;
  }
  const media = element(node.kind === 'image' ? 'img' : node.kind, undefined, 'media-preview');
  media.src = '/graph/media?' + new URLSearchParams({projectId,path:node.sourcePath});
  if (node.kind === 'image') media.alt = node.label;
  else { media.controls = true; media.preload = 'metadata'; if (node.kind === 'video') media.playsInline = true; }
  details.append(media);
}
function nodeHeight(node) { return 38 + labelLines(node.label).length * 16; }
function svgNode(svg, node, x, y, selected, onClick) {
  const ns = 'http://www.w3.org/2000/svg', group = document.createElementNS(ns,'g');
  group.setAttribute('class', `graph-node ${kindClass(node.kind)}${selected ? ' selected' : ''}`);
  group.setAttribute('tabindex','0'); group.setAttribute('role','button');
  group.setAttribute('aria-label',node.label); group.style.cursor = 'pointer';
  const rect = document.createElementNS(ns,'rect');
  for (const [key,value] of Object.entries({x,y,width:200,height:nodeHeight(node),rx:10})) rect.setAttribute(key,value);
  if (selected) rect.style.strokeWidth = '4';
  const tooltip = document.createElementNS(ns,'title'); tooltip.textContent = node.label;
  const title = document.createElementNS(ns,'text'); title.setAttribute('x',x+10);
  labelLines(node.label).forEach((line,index)=>{
    const part=document.createElementNS(ns,'tspan');
    part.setAttribute('x',x+10); part.setAttribute('y',y+22+index*16);
    part.textContent=line; title.append(part);
  });
  const kind = document.createElementNS(ns,'text');
  kind.setAttribute('x',x+10); kind.setAttribute('y',y+nodeHeight(node)-12); kind.setAttribute('class','edge-label'); kind.textContent = kindName(node.kind);
  group.append(tooltip,rect,title,kind);
  group.addEventListener('click',onClick);
  group.addEventListener('keydown',event=>{if(event.key==='Enter'||event.key===' '){event.preventDefault();onClick()}});
  svg.append(group);
}
function relationGraph(data) {
  relationData = data;
  graph.replaceChildren();
  const tools = document.querySelector('#graph-tools'), toggle = document.querySelector('#toggle-relations');
  tools.hidden = false;
  document.querySelector('#relation-count').textContent = `들어오는 관계 ${data.incoming.length}개 · 나가는 관계 ${data.outgoing.length}개${data.truncated?' · 일부만 조회됨':''}`;
  toggle.hidden = data.incoming.length <= 8 && data.outgoing.length <= 8;
  toggle.textContent = expanded ? '접기' : '더 보기'; toggle.setAttribute('aria-expanded',String(expanded));
  const ns='http://www.w3.org/2000/svg', svg=document.createElementNS(ns,'svg');
  svg.setAttribute('aria-label',`${data.node.label}의 대표 관계`);
  const defs=document.createElementNS(ns,'defs'),marker=document.createElementNS(ns,'marker'),arrow=document.createElementNS(ns,'path');
  for(const [key,value] of Object.entries({id:'relation-arrow',markerWidth:8,markerHeight:8,refX:7,refY:4,orient:'auto'})) marker.setAttribute(key,value);
  arrow.setAttribute('d','M0 0 L8 4 L0 8 Z');arrow.setAttribute('fill','#88a7ca');marker.append(arrow);defs.append(marker);svg.append(defs);
  const incoming=data.incoming.slice(0,expanded?100:8), outgoing=data.outgoing.slice(0,expanded?100:8);
  const place=items=>{let y=20;return items.map(item=>{const position={...item,y};y+=nodeHeight(item.node)+18;return position})};
  const left=place(incoming),right=place(outgoing);
  const height=Math.max(450,...[left,right].map(side=>side.length?side.at(-1).y+nodeHeight(side.at(-1).node)+20:0));
  const centerY=(height-nodeHeight(data.node))/2;
  svg.setAttribute('viewBox',`0 0 760 ${height}`);
  svg.style.height = `${height}px`;
  const nearby=[...left.map(item=>({...item,left:true})),...right.map(item=>({...item,left:false}))];
  nearby.forEach(item=>{
    const x=item.left?20:540, y=item.y;
    const line=document.createElementNS(ns,'line');
    for(const [key,value] of Object.entries({x1:item.left?220:480,y1:item.left?y+nodeHeight(item.node)/2:centerY+nodeHeight(data.node)/2,x2:item.left?280:540,y2:item.left?centerY+nodeHeight(data.node)/2:y+nodeHeight(item.node)/2})) line.setAttribute(key,value);
    line.setAttribute('marker-end','url(#relation-arrow)');
    svg.append(line);
    const itemText=`${item.left?'들어옴':'나감'} · ${relationName(item.edge.relation)} · ${item.node.label}`;
    line.setAttribute('aria-label',itemText);
    const hint=document.createElementNS(ns,'title'); hint.textContent=itemText; line.append(hint);
  });
  svgNode(svg,data.node,280,centerY,true,()=>detail(data.node));
  nearby.forEach(item=>svgNode(svg,item.node,item.left?20:540,item.y,false,()=>select(item.node)));
  graph.append(svg);
}
async function select(node) {
  const mine=++request;
  expanded = false;
  document.querySelectorAll('.result').forEach(button=>button.setAttribute('aria-selected',String(button.dataset.id===node.nodeId)));
  detail(node); message('관계 확인 중…',true);
  try {
    const data = await get('/api/graph/context',{projectId,nodeId:node.nodeId,limit:'100'});
    if(mine!==request) return;
    relationGraph(data); message('선택한 항목을 표시했습니다.');
  } catch(error) { if(mine===request) message(error.message); }
}
async function search(event) {
  event?.preventDefault(); const mine=++request;
  document.querySelector('#previous-page').disabled=true;
  document.querySelector('#next-page').disabled=true;
  message('찾는 중…',true);
  try {
    const data=await get('/api/graph/search',{projectId,q:document.querySelector('#query').value,limit:String(pageSize),offset:String(page*pageSize),kind:selectedKind||'files'});
    if(mine!==request) return;
    const nodes=data.nodes;
    results.replaceChildren(); results.scrollTop=0;
    graph.replaceChildren(); relationData=null; document.querySelector('#graph-tools').hidden=true;
    details.replaceChildren(element('p','왼쪽에서 파일이나 문서를 선택하세요.'));
    document.querySelector('#count').textContent=nodes.length?`${page*pageSize+1}–${page*pageSize+nodes.length}개${data.truncated?' 이상':''}`:'0개';
    pages.hidden=page===0&&!data.truncated;
    document.querySelector('#previous-page').disabled=page===0;
    document.querySelector('#next-page').disabled=!data.truncated;
    document.querySelector('#page-label').textContent=`${page+1}페이지`;
    for(const node of nodes){
      const button=element('button',undefined,`result ${kindClass(node.kind)}`); button.type='button'; button.dataset.id=node.nodeId;
      button.append(element('strong',node.label),element('small',`${kindName(node.kind)} · ${node.sourcePath || ''}`));
      button.addEventListener('click',()=>select(node)); results.append(button);
    }
    if(!nodes.length) results.append(element('p','결과가 없습니다. 색인 여부를 확인하세요.','empty'));
    message(`${page+1}페이지 · 검색 결과 ${nodes.length}개${data.truncated?' 이상':''}`);
  } catch(error) {if(mine===request){message(error.message); results.replaceChildren(element('p','검색할 수 없습니다. 색인을 확인하세요.','empty'));}}
}
document.querySelector('#search-form').addEventListener('submit',event=>{page=0;search(event)});
document.querySelector('#previous-page').addEventListener('click',()=>{if(page>0){page--;search()}});
document.querySelector('#next-page').addEventListener('click',()=>{page++;search()});
document.querySelector('#toggle-relations').addEventListener('click',()=>{if(relationData){expanded=!expanded;relationGraph(relationData)}});
document.querySelector('.legend').addEventListener('click', event => {
  const button = event.target.closest('button[data-kind]'); if (!button) return;
  page = 0;
  selectedKind = selectedKind === button.dataset.kind ? '' : button.dataset.kind;
  document.querySelectorAll('.legend button').forEach(item => item.setAttribute('aria-pressed', String(item.dataset.kind === selectedKind)));
  graph.replaceChildren(); relationData=null; document.querySelector('#graph-tools').hidden=true;
  details.replaceChildren(element('p','왼쪽에서 파일이나 문서를 선택하세요.'));
  if (indexed) search();
});
indexForm.addEventListener('change', () => {
  const selected = indexForm.elements.scope.value === 'folders';
  document.querySelector('#index-folders').hidden = !selected;
  document.querySelector('#scope-note').hidden = !selected;
});
indexForm.addEventListener('submit', async event => {
  event.preventDefault();
  const selected = indexForm.elements.scope.value === 'folders';
  const folders = selected ? [...indexForm.querySelectorAll('input[name="folder"]:checked')].map(input => input.value) : [];
  const note = document.querySelector('#index-status');
  if (selected && !folders.length) { note.textContent = '폴더를 하나 이상 선택하세요.'; return; }
  const button = indexForm.querySelector('button[type="submit"]'); button.disabled = true;
  note.textContent = '색인 중…'; window.smsrLoading?.show(note, 'connecting', '색인 중…');
  try {
    let allowLargeReduction = false;
    for (;;) {
      const response = await fetch('/api/graph/index', {method:'POST',headers:{'Content-Type':'application/json'},
        body:JSON.stringify({projectId,folders,allowLargeReduction})});
      const data = await response.json();
      if (response.ok) { location.reload(); return; }
      if (!allowLargeReduction && response.status === 409 && data.error?.includes('절반 이상')
          && confirm('선택한 범위로 바꾸면 기존 색인이 절반 이상 줄어듭니다. 계속할까요?')) {
        allowLargeReduction = true; continue;
      }
      note.textContent = data.error || '색인에 실패했습니다.'; return;
    }
  } catch { note.textContent = '서버 연결에 실패했습니다.'; }
  finally { window.smsrLoading?.hide(note); button.disabled = false; }
});
async function initialize() {
  try {
    const options = await get('/api/graph/index-options',{projectId});
    const folderList = document.querySelector('#index-folders');
    document.querySelector('#index-location').textContent = options.rootPath;
    document.querySelector('#index-title').textContent = options.indexed
      ? `색인 범위 · ${options.selectedFolders.length ? '선택한 폴더' : '프로젝트 전체'}` : '아직 색인되지 않았습니다 · 범위 선택';
    for (const folder of options.folders) {
      const label = element('label'), checkbox = element('input');
      checkbox.type = 'checkbox'; checkbox.name = 'folder'; checkbox.value = folder;
      checkbox.checked = options.selectedFolders.includes(folder);
      label.append(checkbox,document.createTextNode(folder)); folderList.append(label);
    }
    if (options.selectedFolders.length) {
      indexForm.querySelector('input[value="folders"]').checked = true;
      folderList.hidden = false;
      document.querySelector('#scope-note').hidden = false;
    }
    indexPanel.open = !options.indexed || location.hash === '#index-panel';
    indexed = options.indexed;
    if (indexed) search();
    else { message('색인 후 파일과 문서를 찾을 수 있습니다.'); results.replaceChildren(element('p','먼저 색인을 시작하세요.','empty')); }
  } catch(error) {
    indexPanel.open = true; indexForm.hidden = true;
    document.querySelector('#index-title').textContent = '프로젝트 폴더를 찾지 못했습니다';
    document.querySelector('#index-location').textContent = '저장소 경로를 처음 한 번 연결해야 합니다. 아래 상세 관계 도구에서 프로젝트 폴더를 지정하세요.';
    message(error.message);
  }
}
initialize();
