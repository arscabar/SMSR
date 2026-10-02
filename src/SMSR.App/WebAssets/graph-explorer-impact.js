import {text} from './graph-explorer-trace-result.js';
import {relationName} from './graph-explorer-labels.js';
import {evidenceName,resolutionName} from './graph-explorer-evidence.js?v=2';

export async function affected({get,projectId,node,panel,select,sourceLink}){
  panel.replaceChildren(text('p','영향 확인 중…'));
  const relation=document.querySelector('#relation-kind').value;
  const data=await get('/api/graph/affected',{projectId,nodeId:node.nodeId,
    depth:document.querySelector('#trace-depth').value,
    includeInferred:String(document.querySelector('#trace-inferred').checked),...(relation?{relation}:{})});
  const labels=new Map(data.nodes.map(n=>[n.nodeId,n.label]));labels.set(node.nodeId,node.label);
  const content=document.createElement('div');
  content.append(text('h3',`${node.label}의 영향`),text('small',`색인 ${data.revision} · 저장된 정적 관계 기준 · 실행 영향 보장 아님`));
  for(const step of data.steps||[]){
    const item=data.nodes.find(n=>n.nodeId===step.nodeId),card=text('div','');card.className='trace-step';
    const button=text('button',`${step.depth===1?'직접':`${step.depth}단계 간접`} · ${item.label}`);button.type='button';
    button.addEventListener('click',()=>select(item));card.append(button);
    card.append(text('p',`${item.label} → ${labels.get(step.nextId)} · ${relationName(step.edge.relation)}`));
    card.append(text('small',`${evidenceName(step.edge)} · ${resolutionName(step.edge)} · ${step.edge.ownerPath}:${step.edge.sourceLine}`));
    const link=sourceLink({kind:'code',sourcePath:step.edge.ownerPath,line:step.edge.sourceLine});if(link)card.append(link);
    content.append(card);
  }
  if(!data.nodes.length)content.append(text('p','현재 조건에서 들어오는 영향 관계가 없습니다. 누락·미해소 가능성은 진단을 확인하세요.'));
  if(data.truncated)content.append(text('p','탐색 단계·100개 결과 한도로 일부만 표시했습니다.'));
  return content;
}
