export function expansion({get,render,message,projectId}) {
  const button=document.querySelector('#relation-expand');
  let base=null,nodes=new Map(),edges=new Map(),frontier=[],level=1,request=0;
  const add=edge=>edges.set(JSON.stringify(edge),edge);
  function reset(data) {
    request++;base=data;level=1;nodes=new Map([[data.node.nodeId,data.node]]);edges=new Map();
    for(const item of data.items){nodes.set(item.node.nodeId,item.node);add(item.edge);}
    frontier=[...nodes.keys()].filter(id=>id!==data.node.nodeId);
    button.disabled=!frontier.length;button.textContent='연결 한 단계 더';
  }
  button.addEventListener('click',async()=>{
    if(!base||!frontier.length||level>=3)return;
    const mine=++request;button.disabled=true;message('주변 연결 확장 중…',true);
    try {
      // ponytail: bounded neighbourhood, not an unbounded whole-repository canvas.
      const responses=[];
      for(let i=0;i<frontier.length;i+=5) {
        responses.push(...await Promise.all(frontier.slice(i,i+5).map(nodeId=>get('/api/graph/relations',{
          projectId,nodeId,direction:base.direction,limit:'20',offset:'0',revision:String(base.revision),
          ...(base.relation?{relation:base.relation}:{})}))));
        if(mine!==request)return;
      }
      const expanded=new Map(nodes),links=new Map(edges),next=new Set();let partial=false;
      for(const response of responses) {
        if(response.revision!==base.revision)throw new Error('색인이 변경됐습니다. 항목을 다시 선택하세요.');
        partial ||= response.total>response.items.length;
        for(const item of response.items) {
          if(!expanded.has(item.node.nodeId)&&expanded.size>=80){partial=true;continue;}
          const key=JSON.stringify(item.edge);
          if(!links.has(key)&&links.size>=200){partial=true;continue;}
          if(!expanded.has(item.node.nodeId))next.add(item.node.nodeId);
          expanded.set(item.node.nodeId,item.node);links.set(key,item.edge);
        }
      }
      nodes=expanded;edges=links;frontier=[...next];level++;
      render({...base,nodes:[...nodes.values()],edges:[...edges.values()]});
      button.textContent=`${level}단계 표시 · 연결 한 단계 더`;
      message(`${level}단계 · 항목 ${nodes.size}개${partial?' · 일부 관계만 표시, 항목 선택 후 페이지로 확인':''}`);
    }catch(error){if(mine===request)message(error.message);}
    finally{if(mine===request)button.disabled=!frontier.length||level>=3;}
  });
  return {reset,clear(){request++;base=null;button.disabled=true;}};
}
