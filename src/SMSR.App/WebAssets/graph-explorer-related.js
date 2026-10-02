import {text} from './graph-explorer-trace-result.js';
import {relationName} from './graph-explorer-labels.js';

function otherNode(link,node){
  if(link.to.nodeId===node.nodeId)return link.from;
  if(link.from.nodeId===node.nodeId)return link.to;
  return link.to.ownerPath===node.ownerPath&&link.from.ownerPath!==node.ownerPath?link.from:link.to;
}
export async function relatedItems(node,box,{get,projectId,select,sourceLink}){
  const section=text('section','');section.className='related-card';section.append(text('h4','관련 항목'));box.append(section);
  try{
    let links;
    if(['code','symbol'].includes(node.kind)){
      const context=await get('/api/graph/role-context',{projectId,nodeId:node.nodeId});
      links=context.links.map(l=>({edge:l.edge,node:otherNode(l,node),from:l.from.label,to:l.to.label,unavailable:context.unavailable?.includes(l.edge.ownerPath)}));
      if(context.truncated)section.append(text('small','설명용 근거는 일부입니다. 더 넓은 분석은 연결된 에이전트에게 요청하세요.'));
    }else{
      const data=await get('/api/graph/relations',{projectId,nodeId:node.nodeId,limit:'20'});
      links=data.items.filter(i=>!['CONTAINS','DEFINES'].includes(i.edge.relation)).map(i=>({...i,from:i.incoming?i.node.label:node.label,to:i.incoming?node.label:i.node.label}));
    }
    if(!links.length)section.append(text('p','확인된 관련 항목이 없습니다. 분석 범위와 원문을 함께 확인하세요.'));
    const more=text('details','');more.append(text('summary','관련 항목 더 보기'));
    for(const [index,link] of links.entries()){
      const row=text('div','');row.className='related-item';
      const button=text('button',`${link.from} → ${link.to}`);button.type='button';button.onclick=()=>select(link.node);
      const confidence=['EXTRACTED','EXPLICIT'].includes(link.edge.confidence)?'':link.edge.confidence==='AMBIGUOUS'?' · 모호':' · 추정';
      row.append(button,text('small',`${relationName(link.edge.relation)}${confidence}${link.unavailable?' · 원문 변경 또는 근거 확인 불가':''}${link.edge.resolution&&link.edge.resolution!=='RESOLVED'?' · 대상 확인 제한':''} · ${link.edge.ownerPath}:${link.edge.sourceLine}`));
      const source=link.unavailable?null:sourceLink({kind:'code',sourcePath:link.edge.ownerPath,line:link.edge.sourceLine});if(source){source.textContent='관계 원문';row.append(source);}(index<8?section:more).append(row);
    }
    if(links.length>8)section.append(more);
  }catch(e){section.append(text('p',e.message));}
}
