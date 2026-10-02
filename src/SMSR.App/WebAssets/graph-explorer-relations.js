import {expansion} from './graph-explorer-expand.js';
export function relations({get,render,message,projectId,clearGraph=()=>{}}) {
  const expand=expansion({get,render,message,projectId});
  let node=null, revision=null, page=0, request=0;
  const direction=document.querySelector('#relation-direction'), kind=document.querySelector('#relation-kind');
  const previous=document.querySelector('#relation-previous'), next=document.querySelector('#relation-next');
  async function load() {
    if(!node)return;
    expand.clear();
    const mine=++request;
    clearGraph();
    document.querySelector('#graph-tools').hidden=false;
    document.querySelector('#relation-page').textContent='–';
    document.querySelector('#relation-count').textContent='';
    previous.disabled=next.disabled=true;
    message('관계 확인 중…',true);
    try {
      const data=await get('/api/graph/relations',{projectId,nodeId:node.nodeId,
        direction:direction.value,limit:'20',offset:String(page*20),
        ...(kind.value?{relation:kind.value}:{}),...(revision?{revision:String(revision)}:{})});
      if(mine!==request)return;
      revision=data.revision;
      expand.reset(data);
      render({...data,incoming:data.items.filter(i=>i.incoming),outgoing:data.items.filter(i=>!i.incoming)});
      previous.disabled=page===0; next.disabled=(page+1)*20>=data.total;
      document.querySelector('#relation-page').textContent=`${page+1} / ${Math.max(1,Math.ceil(data.total/20))}`;
      document.querySelector('#relation-count').textContent=`선택 조건의 연결 ${data.total}개`;
      message('선택한 항목을 표시했습니다.');
    }catch(error){if(mine===request)message(error.message);}
  }
  previous.addEventListener('click',()=>{if(page>0){page--;load()}});
  next.addEventListener('click',()=>{page++;load()});
  for(const field of [direction,kind])field.addEventListener('change',()=>{page=0;load()});
  return {
    select(value){expand.clear();node=value;page=0;revision=null;return load()},
    clear(){expand.clear();request++;node=null;revision=null;clearGraph();document.querySelector('#graph-tools').hidden=true}
  };
}
