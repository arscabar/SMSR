import {text} from './graph-explorer-trace-result.js';

export function roleCoverage({get,projectId,select}){
  const panel=document.querySelector('#role-coverage'),box=document.querySelector('#role-coverage-content');
  let offset=0,status='',request=0;
  async function load(){
    const mine=++request;box.replaceChildren(text('p','설명 현황 확인 중…'));
    try{
      const data=await get('/api/graph/role-coverage',{projectId,offset,...(status?{status}:{})});if(mine!==request)return;
      panel.querySelector('summary').textContent=`설명 현황 · 최신 ${data.current} / 코드 파일 ${data.totalFiles}`;
      box.replaceChildren();
      for(const [value,label,count] of [['CURRENT','최신 설명',data.current],['MISSING','미분석',data.missing],['STALE','오래됨',data.stale]]){
        const button=text('button',`${label} ${count}`);button.type='button';button.setAttribute('aria-pressed',String(status===value));
        button.onclick=()=>{status=status===value?'':value;offset=0;load();};box.append(button);
      }
      box.append(text('p',`대기 ${data.queued} · 분석 중 ${data.running} · 실패 ${data.failed}`));
      for(const item of data.files){
        const button=text('button',`${item.node.sourcePath} · ${{CURRENT:'최신 설명',MISSING:'미분석',STALE:'오래됨'}[item.status]}`);
        button.type='button';button.className='coverage-file';button.onclick=()=>select(item.node);box.append(button);
      }
      if(!data.files.length)box.append(text('p','해당 상태의 코드 파일이 없습니다.'));
      for(const [label,delta,disabled] of [['이전',-50,offset===0],['다음',50,!data.truncated]]){
        const button=text('button',label);button.type='button';button.disabled=disabled;button.onclick=()=>{offset+=delta;load();};box.append(button);
      }
      const refresh=text('button','현황 다시 확인');refresh.type='button';refresh.onclick=load;box.append(refresh);
    }catch(error){box.replaceChildren(text('p',error.message));const retry=text('button','다시 확인');retry.type='button';retry.onclick=load;box.append(retry);}
  }
  panel.addEventListener('toggle',()=>{if(panel.open)load();});
}
