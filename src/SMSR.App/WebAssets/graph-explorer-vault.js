import {post} from './graph-explorer-sync-http.js';
import {text} from './graph-explorer-trace-result.js';
export function vaultPanel({get,projectId}){
  const find=id=>document.querySelector('#'+id),panel=find('vault-panel'),note=find('vault-status');if(!panel)return;
  let busy=false;
  async function run(action){if(busy)return;busy=true;note.textContent='보관함 처리 중…';try{await action();}catch(e){note.textContent=e.message;}finally{busy=false;}}
  async function load(){
    const data=await get('/api/graph/vault',{projectId});find('vault-sync').disabled=!data.binding;
    find('vault-conflicts').replaceChildren();
    if(!data.binding){note.textContent='보관함을 아직 연결하지 않았습니다.';return;}
    find('vault-path').value=data.binding.vaultPath;find('vault-auto').checked=data.binding.autoSync;
    note.textContent=`${data.binding.vaultPath} · ${data.binding.autoSync?'자동 동기화':'수동 동기화'}${data.error?' · '+data.error:''}`;
    if(data.lastSync){const s=data.lastSync;note.append(text('span',` · 갱신 ${s.written} · 동일 ${s.unchanged} · 원문 제외 ${s.retired} · 충돌 ${s.conflicts.length}`));for(const path of s.conflicts)find('vault-conflicts').append(text('p',`사용자 수정 보호: ${path}`));}
  }
  find('vault-pick').onclick=()=>run(async()=>{const data=await post('/api/graph/vault-pick',{});if(data.path)find('vault-path').value=data.path;note.textContent='폴더를 확인하고 연결을 저장하세요.';});
  find('vault-form').onsubmit=event=>{event.preventDefault();run(async()=>{await post('/api/graph/vault-connect',{projectId,vaultPath:find('vault-path').value,autoSync:find('vault-auto').checked,create:find('vault-create').checked,confirm:find('vault-consent').checked});await load();});};
  find('vault-sync').onclick=()=>run(async()=>{await post('/api/graph/vault-sync',{projectId,action:'sync'});await load();});
  find('vault-refresh').onclick=()=>run(load);panel.addEventListener('toggle',()=>{if(panel.open)run(load);});
}
export async function vaultNote(node,box,{get,projectId}){
  const section=text('p','');box.append(section);
  try{const data=await get('/api/graph/vault-note',{projectId,nodeId:node.nodeId});if(!section.isConnected)return;
    if(!data.uri.startsWith('obsidian://open?path='))return;const link=text('a','Obsidian 분석 노트 열기');link.href=data.uri;link.className='source-action';section.append(link);
  }catch{section.remove();}
}
