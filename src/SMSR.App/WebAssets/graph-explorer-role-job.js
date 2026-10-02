import {text} from './graph-explorer-trace-result.js';

export async function roleJob(node,section,note,{get,projectId,refresh}){
  const button=text('button','설명 생성');button.type='button';section.append(button);
  let timer,job;
  const names={QUEUED:'설명 생성 대기',RUNNING:'Codex가 역할 설명을 분석 중…',FAILED:'설명 생성 실패',SUCCESS:'설명 저장 완료'};
  async function update(){
    if(!section.isConnected){clearTimeout(timer);return;}
    try{
      const wasWaiting=job&&['QUEUED','RUNNING'].includes(job.status);
      job=await get('/api/graph/role-job',{projectId,nodeId:node.nodeId});
      if(job&&job.status!=='NOT_REQUESTED'){note.textContent=job.error||(job.status==='SUCCESS'&&!wasWaiting?'이전 요청 완료 · 현재 근거로 설명을 새로 생성할 수 있습니다.':names[job.status])||'설명 상태 확인 필요';
        button.disabled=['QUEUED','RUNNING'].includes(job.status);button.textContent=job.status==='FAILED'?'설명 다시 생성':'설명 생성';
        if(job.status==='SUCCESS'&&wasWaiting){refresh();return;}
        if(button.disabled)timer=setTimeout(update,2000);}
    }catch(error){note.textContent=error.message;button.disabled=false;}
  }
  button.onclick=async()=>{
    button.disabled=true;
    try{
      const response=await fetch('/api/graph/role-job',{method:'POST',headers:{'Content-Type':'application/json'},
        body:JSON.stringify({projectId,nodeId:node.nodeId,retry:job?.status==='FAILED'})});
      const data=await response.json();if(!response.ok)throw new Error(data.error||'요청 실패');
      job=data;note.textContent=names[data.status]||'설명 요청 저장';await update();
    }catch(error){note.textContent=error.message;button.disabled=false;}
  };
  section.append(text('small','현재 로그인된 Codex로 제한된 코드 근거를 분석합니다. Codex 사용량을 소비합니다.'));
  await update();
}
