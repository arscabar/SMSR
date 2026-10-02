import { kindName, kindClass } from './graph-explorer-labels.js';
import { drawRelations,clearRelationGraph } from './graph-explorer-render.js';
import { relations } from './graph-explorer-relations.js';
import { tracing } from './graph-explorer-trace.js';
import { explanations } from './graph-explorer-diagnostics.js';
import {overview} from './graph-explorer-overview.js';
import {structure} from './graph-explorer-structure.js';
import {semanticDetails} from './graph-explorer-semantic.js';
import {knowledge} from './graph-explorer-knowledge.js';
import {mediaAnalysis} from './graph-explorer-media.js';
import {selectionExport} from './graph-explorer-export.js';
import {roleDetails} from './graph-explorer-role.js';
import {relatedItems} from './graph-explorer-related.js';
import {symbolTree} from './graph-explorer-symbols.js';
import {roleCoverage} from './graph-explorer-role-coverage.js';
import {roleBatch} from './graph-explorer-role-batch.js';
import {vaultPanel,vaultNote} from './graph-explorer-vault.js';

const root = document.querySelector('#explorer');
const projectId = root.dataset.project, workflowId = root.dataset.workflow;
const results = document.querySelector('#results'), details = document.querySelector('#details');
const graph = document.querySelector('#graph');
const status = document.querySelector('#status');
const indexPanel = document.querySelector('#index-panel'), indexForm = document.querySelector('#index-form');
let request = 0, selectedKind = '', indexed = false, page = 0, relationData = null, selectedNode=null;
const relationPanel=document.querySelector('#relation-panel');
relationPanel.addEventListener('toggle',()=>{if(relationPanel.open&&selectedNode)relationControl.select(selectedNode);});
const pageSize = 60, pages = document.querySelector('#search-pages');
const relationControl=relations({get,render:relationGraph,message,projectId,clearGraph:()=>{
  clearRelationGraph(graph);graph.replaceChildren();relationData=null;
  document.querySelector('#edge-details').replaceChildren();
}});
const traceControl=tracing({get,projectId,select,sourceLink});
const explain=explanations({get,projectId,sourceLink});
overview({get,projectId,select,sourceLink});
structure({get,projectId,select});
knowledge({get,projectId,select});
roleCoverage({get,projectId,select});
roleBatch({get,projectId});vaultPanel({get,projectId});

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
  const link = element('a', ['code','symbol'].includes(node.kind) ? '코드 원문 열기' : '문서 원문 열기', 'source-action');
  const document = /\.(pdf|docx|xlsx|png|jpg|jpeg|gif|webp|bmp|mp4|webm|ogv|mp3|wav|ogg|m4a)$/i.test(node.sourcePath) || ['concept','requirement','rationale'].includes(node.kind);
  link.href = (document ? '/graph/document?' : '/graph/source?') + new URLSearchParams({projectId, path:node.sourcePath,
    line:String(Math.max(1,node.line || 1)),...(workflowId?{workflowId}:{})});
  return link;
}
function detail(node) {
  details.replaceChildren(element('h3', node.label));
  details.append(element('p', `종류: ${kindName(node.kind)}`), element('p', `위치: ${node.sourcePath || '없음'}${node.line ? ':' + node.line : ''}`));
  const link = sourceLink(node); if (link) details.append(link);
  vaultNote(node,details,{get,projectId});
  roleDetails(node,details,{get,projectId,sourceLink});
  symbolTree(node,details,{get,projectId,select});
  relatedItems(node,details,{get,projectId,select,sourceLink});
  const tools=element('details');tools.append(element('summary','추가 확인'));
  details.append(tools);traceControl.node(node,tools);
  explain(node);
  semanticDetails(node,details,{get,projectId,sourceLink});
  mediaAnalysis(node,details,{get,projectId});
  selectionExport(node,tools,projectId);
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
function relationGraph(data) {
  relationData = data;
  drawRelations(data,{graph,details:document.querySelector('#edge-details'),sourceLink,select,detail});
}
async function select(node) {
  ++request;
  selectedNode=node;document.querySelector('#edge-details').replaceChildren();
  document.querySelectorAll('.result').forEach(button=>button.setAttribute('aria-selected',String(button.dataset.id===node.nodeId)));
  detail(node);
  relationControl.clear();clearRelationGraph(graph);graph.replaceChildren();
  if(relationPanel.open)await relationControl.select(node);
}
async function search(event) {
  event?.preventDefault(); const mine=++request;
  document.querySelector('#previous-page').disabled=true;
  document.querySelector('#next-page').disabled=true;
  message('찾는 중…',true);
  try {
    const data=await get('/api/graph/files',{projectId,q:document.querySelector('#query').value,limit:String(pageSize),offset:String(page*pageSize),...(selectedKind?{kind:selectedKind}:{})});
    if(mine!==request) return;
    const nodes=data.nodes;
    results.replaceChildren(); results.scrollTop=0;
    selectedNode=null;document.querySelector('#edge-details').replaceChildren();
    clearRelationGraph(graph);graph.replaceChildren(); relationData=null; relationControl.clear();
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
document.querySelector('.legend').addEventListener('click', event => {
  const button = event.target.closest('button[data-kind]'); if (!button) return;
  page = 0;
  selectedKind = selectedKind === button.dataset.kind ? '' : button.dataset.kind;
  document.querySelectorAll('.legend button').forEach(item => item.setAttribute('aria-pressed', String(item.dataset.kind === selectedKind)));
  graph.replaceChildren(); relationData=null; relationControl.clear();
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
  note.textContent = '색인·관계 분석 중…'; window.smsrLoading?.show(note, 'connecting', '색인·관계 분석 중…');
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
