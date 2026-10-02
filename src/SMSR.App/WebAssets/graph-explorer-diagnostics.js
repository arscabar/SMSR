import {text} from './graph-explorer-trace-result.js';
import {deepStatus} from './graph-explorer-deep.js';

export function explanations({get,projectId,sourceLink}){
  const panel=document.querySelector('#diagnostics'),body=document.querySelector('#issue-list');
  let owner=null,offset=0,revision=null,sequence=0;
  async function load(){
    const mine=++sequence;body.replaceChildren(text('p','진단 확인 중…'));
    try{
      const data=await get('/api/graph/issues',{projectId,offset:String(offset),...(owner?{ownerPath:owner}:{})});
      if(mine!==sequence)return;
      if(revision&&revision!==data.revision){offset=0;revision=null;await load();return;}
      revision=data.revision;body.replaceChildren();
      document.querySelector('#issue-count').textContent=`${owner||'프로젝트 전체'} · ${data.total}건 · ${Math.floor(offset/20)+1}페이지`;
      document.querySelector('#issue-previous').disabled=offset===0;
      document.querySelector('#issue-next').disabled=offset+20>=data.total;
      for(const issue of data.items){const card=text('div',issue.reason);card.className='trace-step';
        card.append(text('small',`${issue.relation} · ${issue.ownerPath}:${issue.sourceLine}`));
        const link=sourceLink({kind:'code',sourcePath:issue.ownerPath,line:issue.sourceLine});if(link)card.append(link);
        body.append(card);}
      if(!data.total)body.append(text('p','기록된 진단이 없습니다. 전체 분석의 정확성 보장은 아닙니다.'));
      const deep=text('div','');body.append(deep);deepStatus(deep,get,projectId);
    }catch(e){if(mine===sequence)body.replaceChildren(text('p',e.message));}
  }
  function open(path){owner=path;offset=0;revision=null;document.querySelector('#extra-tools').open=true;panel.open=true;load();}
  panel.addEventListener('toggle',()=>{if(panel.open&&!body.children.length)load()});
  document.querySelector('#issue-previous').addEventListener('click',()=>{offset=Math.max(0,offset-20);load()});
  document.querySelector('#issue-next').addEventListener('click',()=>{offset+=20;load()});
  document.querySelector('#issue-all').addEventListener('click',()=>open(null));
  return async function explain(node){
    const box=text('details','');box.append(text('summary','분석 근거'));document.querySelector('#details').append(box);
    try{
      const data=await get('/api/graph/explain',{projectId,nodeId:node.nodeId});
      box.append(text('p',`색인 ${data.revision} · 들어옴 ${data.incoming} · 나감 ${data.outgoing}`));
      box.append(text('p',data.sourceStatus==='CURRENT'?'현재 원문과 일치':data.sourceStatus==='STALE_OR_UNAVAILABLE'?'원문 변경 또는 접근 불가 · 재색인 필요':'원문 상태는 원문 열기에서 확인'));
      const button=text('button',`이 파일의 진단 ${data.diagnostics.total}건`);button.type='button';button.addEventListener('click',()=>open(node.ownerPath));box.append(button);
    }catch(e){box.append(text('p',e.message));}
  };
}
