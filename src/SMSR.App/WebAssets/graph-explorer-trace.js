import {text,traceResult} from './graph-explorer-trace-result.js';
import {affected} from './graph-explorer-impact.js';

export function tracing({get,projectId,select,sourceLink}){
  let start=null,target=null,impactNode=null,sequence=0;
  const tools=document.querySelector('#trace-tools'), panel=document.querySelector('#trace-result');
  const inferred=document.querySelector('#trace-inferred'),depth=document.querySelector('#trace-depth');
  async function load(){
    if(impactNode){const mine=++sequence;
      try{const content=await affected({get,projectId,node:impactNode,panel,select,sourceLink});if(mine===sequence)panel.replaceChildren(content);}
      catch(e){if(mine===sequence)panel.replaceChildren(text('p',e.message));}return;}
    if(!start||!target)return;
    const mine=++sequence;panel.replaceChildren(text('p','연결 경로 확인 중…'));
    window.smsrLoading?.show(panel,'searching','연결 경로 확인 중…');
    try{
      const relation=document.querySelector('#relation-kind').value;
      const data=await get('/api/graph/trace',{projectId,fromId:start.nodeId,toId:target.nodeId,
        depth:depth.value,includeInferred:String(inferred.checked),...(relation?{relation}:{})});
      if(mine===sequence)traceResult(data,panel,select,sourceLink);
    }catch(e){if(mine===sequence)panel.replaceChildren(text('p',e.message));}
    finally{if(mine===sequence)window.smsrLoading?.hide(panel);}
  }
  for(const input of [inferred,depth,document.querySelector('#relation-kind')])input.addEventListener('change',load);
  document.querySelector('#trace-reset').addEventListener('click',()=>{sequence++;start=target=impactNode=null;tools.hidden=true;panel.replaceChildren();window.smsrLoading?.hide(panel);});
  return {node(node,container=document.querySelector('#details')){
    const box=document.createElement('div');box.className='trace-actions';
    const from=text('button','경로 시작'),to=text('button','여기까지 경로');from.type=to.type='button';
    from.addEventListener('click',()=>{sequence++;start=node;target=impactNode=null;tools.hidden=false;
      document.querySelector('#extra-tools').open=true;
      document.querySelector('#trace-start').textContent=`시작: ${node.label}`;
      panel.replaceChildren(text('p','도착할 항목을 선택하고 ‘여기까지 경로’를 누르세요.'));to.disabled=false;});
    to.disabled=!start;to.addEventListener('click',()=>{document.querySelector('#extra-tools').open=true;target=node;impactNode=null;load()});
    const impact=text('button','영향 보기');impact.type='button';
    impact.addEventListener('click',()=>{impactNode=node;tools.hidden=false;
      document.querySelector('#extra-tools').open=true;
      document.querySelector('#trace-start').textContent=`영향: ${node.label}`;
      load();});
    box.append(from,to,impact);container.append(box);
  }};
}
