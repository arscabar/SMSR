import {post} from './graph-explorer-sync-http.js';
import {batchPreview} from './graph-explorer-batch-preview.js';
export function roleBatch({get,projectId}){
  const find=id=>document.querySelector('#'+id),panel=find('role-batch-panel'),note=find('batch-status');if(!panel)return;
  let preview=null,timer,loaded=false,busy=false;
  const start=find('batch-start');
  function ready(){start.disabled=busy||!preview||!(preview.eligible+preview.current)||!find('batch-consent').checked;}
  async function run(action){if(busy)return;busy=true;ready();try{await action();}catch(e){note.textContent=e.message;}finally{busy=false;ready();}}
  async function load(){
    clearTimeout(timer);const data=await get('/api/graph/role-batch',{projectId});
    for(const action of ['pause','resume','retry'])find('batch-'+action).disabled=!data.batch||(action==='retry'&&!data.failed);
    if(!data.batch){note.textContent='일괄 작업을 시작하지 않았습니다.';return;}
    const total=data.batch.items.length,done=data.success+data.failed;
    find('batch-progress').max=Math.max(1,total);find('batch-progress').value=done;
    const state=done===total?(data.failed?'일부 실패':'완료'):data.batch.paused?'일시정지 · 실행 중인 분석은 마무리합니다.':'진행';
    note.textContent=`${state} ${done}/${total} · 성공 ${data.success} · 실패 ${data.failed} · 분석 중 ${data.running} · 대기 ${data.pending+data.queued} · 최신 재사용 ${data.batch.reused}`;
    if(panel.open&&!data.batch.paused&&done<total)timer=setTimeout(()=>run(load),3000);
  }
  find('batch-preview').onclick=()=>run(async()=>{
    preview=null;find('batch-preview-content').replaceChildren();note.textContent='대상과 원문 근거 확인 중…';find('batch-consent').checked=false;
    preview=await post('/api/graph/role-batch-preview',{projectId,folders:find('batch-folder').value?[find('batch-folder').value]:[],expectedRevision:0});
    batchPreview(find('batch-preview-content'),preview);note.textContent='대상을 확인하고 동의하면 시작할 수 있습니다.';
  });
  find('batch-folder').onchange=()=>{preview=null;find('batch-preview-content').replaceChildren();ready();};find('batch-consent').onchange=ready;
  start.onclick=()=>run(async()=>{await post('/api/graph/role-batch',{projectId,folders:find('batch-folder').value?[find('batch-folder').value]:[],expectedRevision:preview.revision,confirm:true});preview=null;await load();});
  for(const action of ['pause','resume','retry'])find('batch-'+action).onclick=()=>run(async()=>{await post('/api/graph/role-batch-control',{projectId,action});await load();});
  find('batch-refresh').onclick=()=>run(load);
  panel.addEventListener('toggle',()=>{clearTimeout(timer);if(panel.open)run(async()=>{
    if(!loaded){const data=await get('/api/graph/index-options',{projectId});for(const folder of data.folders)find('batch-folder').add(new Option(folder,folder));loaded=true;}await load();
  });});
}
